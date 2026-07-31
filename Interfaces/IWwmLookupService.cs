using IchigoHoshimiya.Entities.Wwm;

namespace IchigoHoshimiya.Interfaces;

// Snapshot is the stored row describing the player's current build — freshly inserted when
// BuildChanged is true, otherwise the existing row that already covers it.
public record WwmLookupOutcome(WwmPlayerSnapshot Snapshot, bool BuildChanged);

public interface IWwmLookupService
{
    // forceFresh appends ?fresh=1, which bypasses the upstream 4h cache and queues a real scan.
    Task<WwmLookupOutcome> LookupAndStore(
        WwmLookupType type,
        string query,
        bool forceFresh = false,
        CancellationToken cancellationToken = default);
}
