using System.Text.Json;
using System.Text.Json.Serialization;

namespace IchigoHoshimiya.DTO;

public record WwmLookupJobDto
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;

    [JsonPropertyName("type")] public string? Type { get; init; }

    [JsonPropertyName("query")] public string? Query { get; init; }

    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;

    [JsonPropertyName("position")] public int? Position { get; init; }

    [JsonPropertyName("eta")] public double? Eta { get; init; }

    // The envelope's shape shifts with job state — a pending job sends "e_c": null, a finished one
    // sends null for position/eta — so every scalar here stays nullable.
    [JsonPropertyName("scanAhead")] public bool? ScanAhead { get; init; }

    [JsonPropertyName("e_c")] public int? ErrorCode { get; init; }

    [JsonPropertyName("error")] public string? Error { get; init; }

    [JsonPropertyName("needsLogin")] public bool? NeedsLogin { get; init; }

    // Present when the POST is answered straight from the server-side cache (4h TTL) instead of being queued.
    [JsonPropertyName("cached")] public bool? Cached { get; init; }

    [JsonPropertyName("stale")] public bool? Stale { get; init; }

    [JsonPropertyName("stale_at")] public long? StaleAt { get; init; }

    [JsonPropertyName("refresh_id")] public string? RefreshId { get; init; }

    // Epoch milliseconds marking when the underlying scrape happened.
    [JsonPropertyName("updated")] public long? Updated { get; init; }

    [JsonPropertyName("result")] public WwmPlayerResultDto? Result { get; init; }
}

public record WwmPlayerResultDto
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;

    [JsonPropertyName("number_id")] public long NumberId { get; init; }

    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;

    [JsonPropertyName("server")] public int Server { get; init; }

    [JsonPropertyName("region")] public string? Region { get; init; }

    [JsonPropertyName("oversea_tag")] public string? OverseaTag { get; init; }

    [JsonPropertyName("guild_name")] public string? GuildName { get; init; }

    [JsonPropertyName("club_id")] public JsonElement? ClubId { get; init; }

    [JsonPropertyName("school")] public int? School { get; init; }

    [JsonPropertyName("level")] public int Level { get; init; }

    [JsonPropertyName("is_master")] public bool IsMaster { get; init; }

    [JsonPropertyName("create_time")] public double? CreateTime { get; init; }

    [JsonPropertyName("login_time")] public double? LoginTime { get; init; }

    [JsonPropertyName("logout_time")] public double? LogoutTime { get; init; }

    [JsonPropertyName("online_time")] public double? OnlineTime { get; init; }

    [JsonPropertyName("build_power")] public int? BuildPower { get; init; }

    [JsonPropertyName("celestial_pulls")] public int? CelestialPulls { get; init; }

    [JsonPropertyName("solemn_pulls")] public int? SolemnPulls { get; init; }

    [JsonPropertyName("harmonic_pulls")] public int? HarmonicPulls { get; init; }

    [JsonPropertyName("other_legend_pulls")] public int? OtherLegendPulls { get; init; }

    [JsonPropertyName("has_pvp_data")] public bool HasPvpData { get; init; }

    [JsonPropertyName("total_matches")] public int? TotalMatches { get; init; }

    [JsonPropertyName("wins")] public int? Wins { get; init; }

    [JsonPropertyName("losses")] public int? Losses { get; init; }

    [JsonPropertyName("win_rate")] public double? WinRate { get; init; }

    [JsonPropertyName("win_rate_percent")] public double? WinRatePercent { get; init; }

    [JsonPropertyName("winning_streak")] public int? WinningStreak { get; init; }

    [JsonPropertyName("pvp_score")] public int? PvpScore { get; init; }

    [JsonPropertyName("surface_score")] public int? SurfaceScore { get; init; }

    [JsonPropertyName("pvp_grade")] public JsonElement? PvpGrade { get; init; }

    [JsonPropertyName("pvp_small_grade")] public JsonElement? PvpSmallGrade { get; init; }

    [JsonPropertyName("pvp_max_grade")] public JsonElement? PvpMaxGrade { get; init; }

    [JsonPropertyName("pvp_max_small_grade")]
    public JsonElement? PvpMaxSmallGrade { get; init; }

    [JsonPropertyName("pvp_season_id")] public int? PvpSeasonId { get; init; }

    [JsonPropertyName("lunjian_grade")] public JsonElement? LunjianGrade { get; init; }

    [JsonPropertyName("lunjian_small_grade")]
    public JsonElement? LunjianSmallGrade { get; init; }

    [JsonPropertyName("max_wuwo_score")] public double? MaxWuwoScore { get; init; }

    [JsonPropertyName("lunjian_max_streak")]
    public int? LunjianMaxStreak { get; init; }

    [JsonPropertyName("lunjian_total")] public int? LunjianTotal { get; init; }

    [JsonPropertyName("arena_rank")] public int? ArenaRank { get; init; }

    [JsonPropertyName("build")] public WwmBuildDto? Build { get; init; }

    [JsonPropertyName("gameplay_trail")] public JsonElement? GameplayTrail { get; init; }

    [JsonPropertyName("current_scenario")]
    public JsonElement? CurrentScenario { get; init; }

    [JsonPropertyName("raw_data")] public JsonElement? RawData { get; init; }
}

public record WwmBuildDto
{
    [JsonPropertyName("weapons")] public WwmWeaponsDto? Weapons { get; init; }

    [JsonPropertyName("xinfa")] public List<WwmNamedIdDto> Xinfa { get; init; } = [];

    [JsonPropertyName("skills")] public List<WwmSkillDto> Skills { get; init; } = [];

    [JsonPropertyName("gear")] public List<WwmGearDto> Gear { get; init; } = [];
}

public record WwmWeaponsDto
{
    [JsonPropertyName("main")] public WwmNamedIdDto? Main { get; init; }

    [JsonPropertyName("sub")] public WwmNamedIdDto? Sub { get; init; }
}

public record WwmNamedIdDto
{
    [JsonPropertyName("id")] public int Id { get; init; }

    [JsonPropertyName("name")] public string? Name { get; init; }
}

public record WwmSkillDto
{
    [JsonPropertyName("slot")] public int Slot { get; init; }

    [JsonPropertyName("id")] public int Id { get; init; }

    [JsonPropertyName("name")] public string? Name { get; init; }
}

public record WwmGearDto
{
    [JsonPropertyName("slot_name")] public string? SlotName { get; init; }

    [JsonPropertyName("slot_ord")] public int SlotOrd { get; init; }

    [JsonPropertyName("item_no")] public long ItemNo { get; init; }

    [JsonPropertyName("index")] public int? Index { get; init; }

    [JsonPropertyName("durability")] public int? Durability { get; init; }

    [JsonPropertyName("suit")] public int? Suit { get; init; }

    [JsonPropertyName("suit_name")] public string? SuitName { get; init; }

    [JsonPropertyName("retoned")] public bool Retoned { get; init; }

    [JsonPropertyName("retone_history")] public List<long> RetoneHistory { get; init; } = [];

    [JsonPropertyName("retone_no")] public long? RetoneNo { get; init; }

    [JsonPropertyName("retone_ts")] public long? RetoneTs { get; init; }

    [JsonPropertyName("retone_count")] public int? RetoneCount { get; init; }

    [JsonPropertyName("base_attrs")] public List<WwmBaseAttrDto> BaseAttrs { get; init; } = [];

    [JsonPropertyName("affixes")] public List<WwmAffixDto> Affixes { get; init; } = [];
}

public record WwmBaseAttrDto
{
    [JsonPropertyName("raw_name")] public string? RawName { get; init; }

    [JsonPropertyName("name")] public string? Name { get; init; }

    [JsonPropertyName("value")] public double Value { get; init; }
}

public record WwmAffixDto
{
    [JsonPropertyName("id")] public long Id { get; init; }

    [JsonPropertyName("name")] public string? Name { get; init; }

    [JsonPropertyName("value")] public double Value { get; init; }

    [JsonPropertyName("type")] public string? Type { get; init; }

    [JsonPropertyName("active")] public bool? Active { get; init; }

    [JsonPropertyName("display_value")] public string? DisplayValue { get; init; }
}
