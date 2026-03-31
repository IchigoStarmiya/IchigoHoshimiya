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
    private static readonly TimeSpan FourMinutes = TimeSpan.FromMinutes(4);
    private static readonly TimeSpan FiveMinutes = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(15);

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

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _voiceClient = await gatewayClient.JoinVoiceChannelAsync(
                _settings.GuildId,
                _settings.ChannelId,
                new VoiceClientConfiguration(),
                cancellationToken);

            // Subscribe to Ready before calling StartAsync so we never miss the event.
            var readyTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _voiceClient.Ready += () =>
            {
                readyTcs.TrySetResult();
                return ValueTask.CompletedTask;
            };

            // StartAsync connects the WebSocket and sends IDENTIFY.
            // Use CancellationToken.None — the voice connection lifetime is managed
            // by Dispose(), not by the timer's token.
            // If StartAsync faults before Ready fires, propagate that error immediately.
            var startTask = _voiceClient.StartAsync().AsTask();
            _ = startTask.ContinueWith(
                t => readyTcs.TrySetException(t.Exception!.InnerExceptions),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);

            // Wait for the voice server to confirm the connection is ready.
            using var readyTimeoutCts = new CancellationTokenSource(ReadyTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(readyTimeoutCts.Token, cancellationToken);
            await readyTcs.Task.WaitAsync(linkedCts.Token);

            _voiceStream = _voiceClient.CreateVoiceStream(new VoiceStreamConfiguration
            {
                NormalizeSpeed = true
            });

            _timerTask = Task.Run(() => RunLoopAsync(token), CancellationToken.None);
        }
        catch
        {
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

        // Only send the leave update if we actually held a connection.
        if (!hadClient)
            return;

        try
        {
            await gatewayClient.UpdateVoiceStateAsync(new VoiceStateProperties(_settings.GuildId, null));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send voice leave state to Discord");
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
            logger.LogError(ex, "Voice timer loop encountered an unhandled error");
        }
    }

    private async Task PlayClipAsync(string filePath, CancellationToken ct)
    {
        if (_voiceStream is null)
        {
            logger.LogWarning("Skipping clip — voice stream is null");
            return;
        }

        if (!File.Exists(filePath))
        {
            logger.LogWarning("Audio file not found, skipping: {FilePath}", filePath);
            return;
        }

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
            logger.LogError(ex, "Failed to start FFmpeg for {FilePath}", filePath);
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
                try
                {
                    await using var encodeStream = new OpusEncodeStream(
                        _voiceStream,
                        PcmFormat.Short,
                        VoiceChannels.Stereo,
                        OpusApplication.Audio,
                        new OpusEncodeStreamConfiguration { FrameDuration = 20.0f },
                        leaveOpen: true);

                    await ffmpeg.StandardOutput.BaseStream.CopyToAsync(encodeStream, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while streaming audio from {FilePath}", filePath);
                }
                finally
                {
                    try { await ffmpeg.WaitForExitAsync(CancellationToken.None); } catch { /* ignore */ }
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
