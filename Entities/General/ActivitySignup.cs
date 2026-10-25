using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace IchigoHoshimiya.Entities.General;

public enum ActivityChoice
{
    ConquestDungeons = 0,
    Transcendence = 1,
    Abyss = 2,
    BattlefieldArena = 3,
    BurnAltEnergy = 4,
    Available = 5,
    Absent = 6
}

[Table("activity_signup")]
[Index(nameof(SlotStartUtc))]
[Index(nameof(IsOpen), nameof(CloseAtUtc))]
public class ActivitySignup
{
    [Key] [Column("id")] public long Id { get; init; }

    [Column("channel_id")] public ulong ChannelId { get; init; }

    [Column("message_id")] public ulong? MessageId { get; set; }

    [Column("title")] [MaxLength(200)] public required string Title { get; init; }

    [Column("slot_start_utc")] public DateTime SlotStartUtc { get; init; }

    [Column("slot_end_utc")] public DateTime SlotEndUtc { get; init; }

    [Column("close_at_utc")] public DateTime CloseAtUtc { get; init; }

    [Column("is_siege_day")] public bool IsSiegeDay { get; init; }

    // Test signups are posted on demand and never count towards the schedule
    [Column("is_test")] public bool IsTest { get; init; }

    [Column("is_open")] public bool IsOpen { get; set; } = true;

    public List<ActivitySignupEntry> Entries { get; init; } = [];
}

[Table("activity_signup_entry")]
[Index(nameof(ActivitySignupId), nameof(UserId), IsUnique = true)]
public class ActivitySignupEntry
{
    [Key] [Column("id")] public long Id { get; init; }

    [Column("activity_signup_id")] public long ActivitySignupId { get; init; }

    [ForeignKey(nameof(ActivitySignupId))] public ActivitySignup ActivitySignup { get; init; } = null!;

    [Column("user_id")] public ulong UserId { get; init; }

    [Column("choice")] public ActivityChoice Choice { get; set; }

    [Column("updated_at_utc")] public DateTime UpdatedAtUtc { get; set; }
}
