using IchigoHoshimiya.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Renci.SshNet;

namespace IchigoHoshimiya.Services;

public class GameServerSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 22;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PrivateKeyPath { get; set; } = string.Empty;
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
        && !string.IsNullOrWhiteSpace(_settings.PalworldUnit)
        && (!string.IsNullOrWhiteSpace(_settings.Password)
            || ResolvePrivateKeyPath() is not null);

    public Task StartCobbleverseAsync(CancellationToken cancellationToken = default) =>
        SwitchAsync(_settings.CobbleverseUnit, _settings.PalworldUnit, cancellationToken);

    public Task StartPalworldAsync(CancellationToken cancellationToken = default) =>
        SwitchAsync(_settings.PalworldUnit, _settings.CobbleverseUnit, cancellationToken);

    private async Task SwitchAsync(string startUnit, string stopUnit, CancellationToken cancellationToken)
    {
        using var client = new SshClient(CreateConnectionInfo());
        await client.ConnectAsync(cancellationToken);
        try
        {
            // Unit names come from trusted configuration, so they are safe to interpolate.
            var script = $"sudo systemctl stop {stopUnit} && sudo systemctl start {startUnit}";
            using var command = client.CreateCommand(script);
            await command.ExecuteAsync(cancellationToken);

            if (command.ExitStatus is not 0)
            {
                logger.LogError(
                    "systemctl switch failed (start={Start}, stop={Stop}): exit={Exit} {Error}",
                    startUnit, stopUnit, command.ExitStatus, command.Error);
                throw new InvalidOperationException(
                    $"systemctl exited with {command.ExitStatus}: {command.Error}".Trim());
            }

            logger.LogInformation(
                "systemctl switch succeeded (start={Start}, stop={Stop}): {Output}",
                startUnit, stopUnit, command.Result);
        }
        finally
        {
            client.Disconnect();
        }
    }

    private ConnectionInfo CreateConnectionInfo()
    {
        var keyPath = ResolvePrivateKeyPath();
        AuthenticationMethod auth = keyPath is not null
            ? new PrivateKeyAuthenticationMethod(_settings.Username, new PrivateKeyFile(keyPath))
            : new PasswordAuthenticationMethod(_settings.Username, _settings.Password);

        return new ConnectionInfo(_settings.Host, _settings.Port, _settings.Username, auth);
    }

    // Returns the configured key path, or the conventional ~/.ssh key when none is set, or null if neither exists.
    private string? ResolvePrivateKeyPath()
    {
        if (!string.IsNullOrWhiteSpace(_settings.PrivateKeyPath))
        {
            return _settings.PrivateKeyPath;
        }

        var sshDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh");

        return DefaultKeyNames
            .Select(name => Path.Combine(sshDir, name))
            .FirstOrDefault(File.Exists);
    }

    private static readonly string[] DefaultKeyNames =
        ["id_ed25519", "id_ecdsa", "id_rsa"];
}