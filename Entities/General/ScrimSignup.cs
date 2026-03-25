using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace IchigoHoshimiya.Entities.General;

[Flags]
public enum ScrimAvailableDays
{
    None = 0,
    Monday = 1 << 0,
    Tuesday = 1 << 1,
    Wednesday = 1 << 2,
    Thursday = 1 << 3,
    Friday = 1 << 4
}

public enum ScrimRole
{
    Flex = 0,
    Healer = 1,
    Tank = 2,
    Dps = 3
}

public enum ScrimWeapon
{
    Unspecified = 0,
    PanaceaFan = 1,
    NamelessSword = 2,
    TwinbladeRopeDart = 3,
    MoSb = 4,
    MoHybrid = 5
}

[Table("scrim_signup")]
[Index(nameof(MessageId), IsUnique = true)]
[Index(nameof(CreatedById))]
[Index(nameof(CreatedAtUtc))]
public class ScrimSignup
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("channel_id")]
    public ulong ChannelId { get; set; }

    [Column("message_id")]
    public ulong? MessageId { get; set; }

    [Column("created_by_id")]
    public ulong CreatedById { get; init; }

    [Column("created_at_utc")]
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

    [Column("title")]
    [MaxLength(200)]
    public string Title { get; init; } = "SHIRANUI GvG Scrim";

    [Column("is_open")]
    public bool IsOpen { get; set; } = true;

    public List<ScrimSignupEntry> Entries { get; set; } = new();
}

[Table("scrim_signup_entry")]
[Index(nameof(ScrimSignupId), nameof(UserId), IsUnique = true)]
[Index(nameof(UserId))]
public class ScrimSignupEntry
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("scrim_signup_id")]
    public long ScrimSignupId { get; set; }

    [ForeignKey(nameof(ScrimSignupId))]
    public ScrimSignup ScrimSignup { get; set; } = null!;

    [Column("user_id")]
    public ulong UserId { get; set; }

    [Column("role")]
    public ScrimRole Role { get; set; }

    [Column("weapon")]
    public ScrimWeapon Weapon { get; set; } = ScrimWeapon.Unspecified;

    [Column("available_days")]
    public ScrimAvailableDays AvailableDays { get; set; }

    [Column("updated_at_utc")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

