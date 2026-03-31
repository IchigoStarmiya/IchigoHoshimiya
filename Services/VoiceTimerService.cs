using System.Diagnostics;
using IchigoHoshimiya.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord.Gateway;
using NetCord.Gateway.Voice;

namespace IchigoHoshimiya.Services;

public class VoiceTimerService(
    GatewayClient gatewayClient,
    IOptions<VoiceTimerSettings> options,
    ILogger<VoiceTimerService> logger)
    : IVoiceTimerService, IAsyncDisposable
{
    private static readonly TimeSpan FourMinutes = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan FiveMinutes = TimeSpan.FromMinutes(5);

    private readonly VoiceTimerSettings _settings = options.Value;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private VoiceClient? _voiceClient;
    private Stream? _voiceStream;
    private CancellationTokenSource? _cts;
    private Task? _timerTask;

    public bool IsRunning => _timerTask is { IsCompleted: false };

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await StopCoreAsync();

            // Give Discord time to process the voice-leave before immediately rejoining.
            // Without this, the VoiceStateUpdate + VoiceServerUpdate events that
            // JoinVoiceChannelAsync waits for may never arrive and the call times out.
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            logger.LogInformation("VoiceTimer: [1] Sending voice join to gateway (guild={GuildId}, channel={ChannelId})",
                _settings.GuildId, _settings.ChannelId);

            _voiceClient = await gatewayClient.JoinVoiceChannelAsync(
                _settings.GuildId,
                _settings.ChannelId,
                new VoiceClientConfiguration(),
                cancellationToken);

            logger.LogInformation("VoiceTimer: [2] JoinVoiceChannelAsync returned — calling StartAsync");

            // Track whether Ready fires during or after StartAsync.
            var readyTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _voiceClient.Ready += () =>
            {
                logger.LogInformation("VoiceTimer: [Ready] event fired");
                readyTcs.TrySetResult();
                return ValueTask.CompletedTask;
            };

            // Await StartAsync directly (as shown in official docs).
            // Wrap it in a 20-second timeout so a silent hang surfaces as a real error.
            try
            {
                await _voiceClient.StartAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
            }
            catch (TimeoutException)
            {
                throw new TimeoutException(
                    "VoiceClient.StartAsync did not complete within 20 s. " +
                    "The bot likely cannot reach Discord's voice servers — check outbound UDP/WSS connectivity.");
            }

            logger.LogInformation("VoiceTimer: [3] StartAsync returned");

            // If Ready has not fired yet, wait up to 5 more seconds for it.
            if (!readyTcs.Task.IsCompleted)
            {
                logger.LogInformation("VoiceTimer: [4] Ready not yet fired — waiting up to 5 s");
                try
                {
                    await readyTcs.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
                }
                catch (TimeoutException)
                {
                    logger.LogWarning("VoiceTimer: Ready event did not fire after StartAsync — proceeding anyway");
                }
            }
            else
            {
                logger.LogInformation("VoiceTimer: [4] Ready had already fired during StartAsync");
            }

            logger.LogInformation("VoiceTimer: [5] Entering speaking state");

            await _voiceClient.EnterSpeakingStateAsync(new SpeakingProperties(SpeakingFlags.Microphone), cancellationToken: cancellationToken);

            logger.LogInformation("VoiceTimer: [6] Creating voice stream and starting timer loop");

            _voiceStream = _voiceClient.CreateVoiceStream(new VoiceStreamConfiguration
            {
                NormalizeSpeed = true
            });

            _timerTask = Task.Run(() => RunLoopAsync(token), CancellationToken.None);

            logger.LogInformation("VoiceTimer: [6] Timer loop started");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "VoiceTimer: StartAsync failed");
            await StopCoreAsync();
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await StopCoreAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task StopCoreAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
            _cts = null;
        }

        if (_timerTask is not null)
        {
            try { await _timerTask.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch { /* cancellation or timeout — expected */ }
            _timerTask = null;
        }

        if (_voiceStream is not null)
        {
            try { await _voiceStream.DisposeAsync(); } catch { /* ignore */ }
            _voiceStream = null;
        }

        var hadClient = _voiceClient is not null;

        if (_voiceClient is not null)
        {
            try { _voiceClient.Dispose(); } catch { /* ignore */ }
            _voiceClient = null;
        }

        if (!hadClient)
            return;

        try
        {
            await gatewayClient.UpdateVoiceStateAsync(new VoiceStateProperties(_settings.GuildId, null));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "VoiceTimer: Failed to send voice leave state to Discord");
        }
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        try
        {
            await PlayClipAsync(_settings.StartClipPath, ct);

            await Task.Delay(FourMinutes, ct);

            await PlayClipAsync(_settings.FourMinClipPath, ct);

            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(FiveMinutes, ct);
                await PlayClipAsync(_settings.FiveMinClipPath, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Normal stop.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "VoiceTimer: Timer loop encountered an unhandled error");
        }
    }

    private async Task PlayClipAsync(string filePath, CancellationToken ct)
    {
        if (_voiceStream is null)
        {
            logger.LogWarning("VoiceTimer: Skipping clip — voice stream is null");
            return;
        }

        if (!File.Exists(filePath))
        {
            logger.LogWarning("VoiceTimer: Audio file not found, skipping: {FilePath}", filePath);
            return;
        }

        if (_voiceClient is not null)
            await _voiceClient.EnterSpeakingStateAsync(new SpeakingProperties(SpeakingFlags.Microphone), cancellationToken: ct);

        Process ffmpeg;
        try
        {
            ffmpeg = Process.Start(new ProcessStartInfo
            {
                FileName = _settings.FfmpegPath,
                Arguments = $"-hide_banner -loglevel error -i \"{filePath}\" -ac 2 -ar 48000 -f s16le pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }) ?? throw new InvalidOperationException("Process.Start returned null.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "VoiceTimer: Failed to start FFmpeg for {FilePath}", filePath);
            return;
        }

        try
        {
            await using (ct.Register(() =>
            {
                try { ffmpeg.Kill(entireProcessTree: true); }
                catch { /* process already exited */ }
            }))
            {
                // Drain stderr in the background to prevent FFmpeg blocking on a full pipe buffer.
                var stderrTask = ffmpeg.StandardError.ReadToEndAsync(CancellationToken.None);

                try
                {
                    await using var encodeStream = new OpusEncodeStream(
                        _voiceStream,
                        PcmFormat.Short,
                        VoiceChannels.Stereo,
                        OpusApplication.Audio,
                        new OpusEncodeStreamConfiguration { FrameDuration = 20.0f },
                        leaveOpen: true);

                    var countingStream = new ByteCountingStream(ffmpeg.StandardOutput.BaseStream);
                    await countingStream.CopyToAsync(encodeStream, ct);

                    logger.LogInformation("VoiceTimer: Finished streaming {FilePath} ({Bytes} PCM bytes)", filePath, countingStream.BytesRead);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "VoiceTimer: Error while streaming audio from {FilePath}", filePath);
                }
                finally
                {
                    try { await ffmpeg.WaitForExitAsync(CancellationToken.None); } catch { /* ignore */ }
                    var stderr = await stderrTask;
                    if (!string.IsNullOrWhiteSpace(stderr))
                        logger.LogWarning("VoiceTimer: FFmpeg stderr for {FilePath}: {Stderr}", filePath, stderr);
                    logger.LogInformation("VoiceTimer: FFmpeg exit code {ExitCode} for {FilePath}", ffmpeg.ExitCode, filePath);
                }
            }
        }
        finally
        {
            ffmpeg.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopCoreAsync();
        _lock.Dispose();
    }
}

file sealed class ByteCountingStream(Stream inner) : Stream
{
    public long BytesRead { get; private set; }

    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override int Read(byte[] buffer, int offset, int count)
    {
        var n = inner.Read(buffer, offset, count);
        BytesRead += n;
        return n;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var n = await inner.ReadAsync(buffer, cancellationToken);
        BytesRead += n;
        return n;
    }
}
