namespace IchigoHoshimiya.Interfaces;

public interface IHetznerService
{
    bool IsConfigured { get; }
    Task StartServerAsync(CancellationToken cancellationToken = default);
    Task StopServerAsync(CancellationToken cancellationToken = default);
}
