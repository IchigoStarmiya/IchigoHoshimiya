using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace IchigoHoshimiya.Entities.Wwm;

[Table("wwm_player")]
[Index(nameof(Name))]
[Index(nameof(ExternalId))]
public class WwmPlayer
{
    [Key]
    [Column("number_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long NumberId { get; init; }

    [Column("external_id")]
    [MaxLength(64)]
    public string ExternalId { get; set; } = string.Empty;

    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("region")]
    [MaxLength(32)]
    public string? Region { get; set; }

    [Column("server")]
    public int Server { get; set; }

    [Column("account_created_at_utc")]
    public DateTime? AccountCreatedAtUtc { get; set; }

    [Column("first_seen_at_utc")]
    public DateTime FirstSeenAtUtc { get; init; } = DateTime.UtcNow;

    [Column("last_seen_at_utc")]
    public DateTime LastSeenAtUtc { get; set; } = DateTime.UtcNow;

    public List<WwmPlayerSnapshot> Snapshots { get; set; } = [];
}
