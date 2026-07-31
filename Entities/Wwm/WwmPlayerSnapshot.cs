using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace IchigoHoshimiya.Entities.Wwm;

public enum WwmLookupType
{
    NumberId = 0,
    Name = 1
}

[Table("wwm_player_snapshot")]
[Index(nameof(NumberId), nameof(FetchedAtUtc), IsUnique = true)]
[Index(nameof(NumberId), nameof(BuildHash))]
[Index(nameof(FetchedAtUtc))]
[Index(nameof(LookupJobId))]
[Index(nameof(GuildName))]
public class WwmPlayerSnapshot
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("number_id")]
    public long NumberId { get; set; }

    [ForeignKey(nameof(NumberId))]
    public WwmPlayer Player { get; set; } = null!;

    // When this bot performed the lookup.
    [Column("fetched_at_utc")]
    public DateTime FetchedAtUtc { get; init; } = DateTime.UtcNow;

    // When the upstream scrape actually ran. Differs from FetchedAtUtc on a cache hit.
    [Column("data_updated_at_utc")]
    public DateTime? DataUpdatedAtUtc { get; set; }

    // SHA-256 over the major build choices only — weapons, inners, mystics, equipped gear and
    // attunements. A snapshot is written only when this differs from the player's previous one, so
    // the table holds one row per distinct build rather than per check or per stat re-roll.
    [Column("build_hash")]
    [MaxLength(64)]
    public string BuildHash { get; set; } = string.Empty;

    [Column("was_cached")]
    public bool WasCached { get; set; }

    [Column("was_stale")]
    public bool WasStale { get; set; }

    [Column("lookup_job_id")]
    [MaxLength(64)]
    public string LookupJobId { get; set; } = string.Empty;

    [Column("lookup_type")]
    public WwmLookupType LookupType { get; set; }

    [Column("lookup_query")]
    [MaxLength(100)]
    public string LookupQuery { get; set; } = string.Empty;

    [Column("external_id")]
    [MaxLength(64)]
    public string ExternalId { get; set; } = string.Empty;

    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("server")]
    public int Server { get; set; }

    [Column("region")]
    [MaxLength(32)]
    public string? Region { get; set; }

    [Column("oversea_tag")]
    [MaxLength(32)]
    public string? OverseaTag { get; set; }

    [Column("guild_name")]
    [MaxLength(100)]
    public string? GuildName { get; set; }

    [Column("club_id")]
    [MaxLength(64)]
    public string? ClubId { get; set; }

    [Column("discord_account_id")]
    public ulong? DiscordAccountId { get; set; }

    [Column("level")]
    public int Level { get; set; }

    [Column("school")]
    public int? School { get; set; }

    [Column("is_master")]
    public bool IsMaster { get; set; }

    [Column("build_power")]
    public int? BuildPower { get; set; }

    [Column("max_xiuwei_kungfu")]
    public int? MaxXiuweiKungfu { get; set; }

    [Column("celestial_pulls")]
    public int? CelestialPulls { get; set; }

    [Column("solemn_pulls")]
    public int? SolemnPulls { get; set; }

    [Column("harmonic_pulls")]
    public int? HarmonicPulls { get; set; }

    [Column("other_legend_pulls")]
    public int? OtherLegendPulls { get; set; }

    [Column("account_created_at_utc")]
    public DateTime? AccountCreatedAtUtc { get; set; }

    [Column("login_at_utc")]
    public DateTime? LoginAtUtc { get; set; }

    [Column("logout_at_utc")]
    public DateTime? LogoutAtUtc { get; set; }

    [Column("online_time_seconds")]
    public double? OnlineTimeSeconds { get; set; }

    [Column("is_online")]
    public bool? IsOnline { get; set; }

    [Column("has_pvp_data")]
    public bool HasPvpData { get; set; }

    [Column("total_matches")]
    public int? TotalMatches { get; set; }

    [Column("wins")]
    public int? Wins { get; set; }

    [Column("losses")]
    public int? Losses { get; set; }

    [Column("win_rate")]
    public double? WinRate { get; set; }

    [Column("win_rate_percent")]
    public double? WinRatePercent { get; set; }

    [Column("winning_streak")]
    public int? WinningStreak { get; set; }

    [Column("pvp_score")]
    public int? PvpScore { get; set; }

    [Column("surface_score")]
    public int? SurfaceScore { get; set; }

    [Column("pvp_grade")]
    [MaxLength(64)]
    public string? PvpGrade { get; set; }

    [Column("pvp_small_grade")]
    [MaxLength(64)]
    public string? PvpSmallGrade { get; set; }

    [Column("pvp_max_grade")]
    [MaxLength(64)]
    public string? PvpMaxGrade { get; set; }

    [Column("pvp_max_small_grade")]
    [MaxLength(64)]
    public string? PvpMaxSmallGrade { get; set; }

    [Column("pvp_season_id")]
    public int? PvpSeasonId { get; set; }

    [Column("lunjian_grade")]
    [MaxLength(64)]
    public string? LunjianGrade { get; set; }

    [Column("lunjian_small_grade")]
    [MaxLength(64)]
    public string? LunjianSmallGrade { get; set; }

    [Column("max_wuwo_score")]
    public double? MaxWuwoScore { get; set; }

    [Column("lunjian_max_streak")]
    public int? LunjianMaxStreak { get; set; }

    [Column("lunjian_total")]
    public int? LunjianTotal { get; set; }

    [Column("arena_rank")]
    public int? ArenaRank { get; set; }

    [Column("main_weapon_id")]
    public int? MainWeaponId { get; set; }

    [Column("main_weapon_name")]
    [MaxLength(100)]
    public string? MainWeaponName { get; set; }

    [Column("sub_weapon_id")]
    public int? SubWeaponId { get; set; }

    [Column("sub_weapon_name")]
    [MaxLength(100)]
    public string? SubWeaponName { get; set; }

    [Column("gameplay_trail_json")]
    public string? GameplayTrailJson { get; set; }

    [Column("current_scenario_json")]
    public string? CurrentScenarioJson { get; set; }

    [Column("raw_base_json")]
    public string? RawBaseJson { get; set; }

    public List<WwmGear> Gear { get; set; } = [];

    public List<WwmSnapshotXinfa> Xinfa { get; set; } = [];

    public List<WwmSnapshotSkill> Skills { get; set; } = [];
}
