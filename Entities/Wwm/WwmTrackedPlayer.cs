using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace IchigoHoshimiya.Entities.Wwm;

[Table("wwm_tracked_player")]
[Index(nameof(Enabled))]
[Index(nameof(LastCheckedAtUtc))]
public class WwmTrackedPlayer
{
    [Key]
    [Column("number_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long NumberId { get; init; }

    [Column("label")]
    [MaxLength(100)]
    public string? Label { get; set; }

    [Column("added_by_id")]
    public ulong AddedById { get; init; }

    [Column("added_at_utc")]
    public DateTime AddedAtUtc { get; init; } = DateTime.UtcNow;

    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    [Column("last_checked_at_utc")]
    public DateTime? LastCheckedAtUtc { get; set; }

    [Column("last_success_at_utc")]
    public DateTime? LastSuccessAtUtc { get; set; }

    [Column("last_build_change_at_utc")]
    public DateTime? LastBuildChangeAtUtc { get; set; }

    [Column("last_error")]
    [MaxLength(500)]
    public string? LastError { get; set; }
}
