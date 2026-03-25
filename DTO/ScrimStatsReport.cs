using IchigoHoshimiya.Entities.General;

namespace IchigoHoshimiya.DTO;

public sealed class ScrimStatsReport
{
    public required long SignupId { get; init; }

    public required ulong MessageId { get; init; }

    public required int TotalUniqueSignups { get; init; }

    public required IReadOnlyList<ScrimDayStats> DayStats { get; init; }

    public required IReadOnlyList<ScrimDayStats> RecommendedDays { get; init; }
}

public sealed class ScrimDayStats
{
    public required ScrimAvailableDays Day { get; init; }

    public required int TotalSignups { get; init; }

    public required IReadOnlyDictionary<ScrimRole, int> RoleCounts { get; init; }

    public required IReadOnlyDictionary<ScrimRole, IReadOnlyDictionary<ScrimWeapon, int>> WeaponCountsByRole { get; init; }

    public required IReadOnlyList<ScrimSignupEntry> DayEntries { get; init; }
}

