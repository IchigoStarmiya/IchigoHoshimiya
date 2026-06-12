using System.Diagnostics;
using IchigoHoshimiya.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IchigoHoshimiya.Services;

public class GameServerSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 22;
    public string Username { get; set; } = string.Empty;
    public string CobbleverseUnit { get; set; } = string.Empty;
    public string PalworldUnit { get; set; } = string.Empty;
}

public class GameServerService(
    IOptions<GameServerSettings> options,
    ILogger<GameServerService> logger)
    : IGameServerService
{
    private readonly GameServerSettings _settings = options.Value;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.Host)
        && !string.IsNullOrWhiteSpace(_settings.Username)
        && !string.IsNullOrWhiteSpace(_settings.CobbleverseUnit)
        && !string.IsNullOrWhiteSpace(_settings.PalworldUnit);

    public Task StartCobbleverseAsync(CancellationToken cancellationToken = default) =>
        SwitchAsync(_settings.CobbleverseUnit, _settings.PalworldUnit, cancellationToken);

    public Task StartPalworldAsync(CancellationToken cancellationToken = default) =>
        SwitchAsync(_settings.PalworldUnit, _settings.CobbleverseUnit, cancellationToken);

    private async Task SwitchAsync(string startUnit, string stopUnit, CancellationToken cancellationToken)
    {
        // Unit names come from trusted configuration, so they are safe to interpolate.
        var remoteCommand = $"sudo systemctl stop {stopUnit} && sudo systemctl start {startUnit}";

        var startInfo = new ProcessStartInfo
        {
            FileName = "ssh",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        // Let the host's own ssh setup (keys, agent, known_hosts) handle auth.
        // BatchMode fails fast instead of hanging on a password prompt; accept-new
        // trusts the server on first connect rather than failing when the bot
        // process runs under a user whose known_hosts doesn't have it yet.
        startInfo.ArgumentList.Add("-p");
        startInfo.ArgumentList.Add(_settings.Port.ToString());
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add("BatchMode=yes");
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add("StrictHostKeyChecking=accept-new");
        startInfo.ArgumentList.Add($"{_settings.Username}@{_settings.Host}");
        startInfo.ArgumentList.Add(remoteCommand);

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var stdout = (await stdoutTask).Trim();
        var stderr = (await stderrTask).Trim();

        if (process.ExitCode is not 0)
        {
            logger.LogError(
                "ssh systemctl switch failed (start={Start}, stop={Stop}): exit={Exit} {Error}",
                startUnit, stopUnit, process.ExitCode, stderr);
            throw new InvalidOperationException(
                $"ssh exited with {process.ExitCode}: {stderr}".Trim());
        }

        logger.LogInformation(
            "ssh systemctl switch succeeded (start={Start}, stop={Stop}): {Output}",
            startUnit, stopUnit, stdout);
    }
}