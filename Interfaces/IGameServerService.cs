namespace IchigoHoshimiya.Interfaces;

public interface IGameServerService
{
    bool IsConfigured { get; }
    Task StartCobbleverseAsync(CancellationToken cancellationToken = default);
    Task StartPalworldAsync(CancellationToken cancellationToken = default);
}