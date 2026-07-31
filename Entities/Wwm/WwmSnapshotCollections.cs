using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace IchigoHoshimiya.Entities.Wwm;

[Table("wwm_snapshot_xinfa")]
[Index(nameof(SnapshotId), nameof(Ordinal), IsUnique = true)]
[Index(nameof(XinfaId))]
public class WwmSnapshotXinfa
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("snapshot_id")]
    public long SnapshotId { get; set; }

    [ForeignKey(nameof(SnapshotId))]
    public WwmPlayerSnapshot Snapshot { get; set; } = null!;

    [Column("ordinal")]
    public int Ordinal { get; set; }

    [Column("xinfa_id")]
    public int XinfaId { get; set; }

    [Column("name")]
    [MaxLength(100)]
    public string? Name { get; set; }
}

[Table("wwm_snapshot_skill")]
[Index(nameof(SnapshotId), nameof(Slot), IsUnique = true)]
[Index(nameof(SkillId))]
public class WwmSnapshotSkill
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("snapshot_id")]
    public long SnapshotId { get; set; }

    [ForeignKey(nameof(SnapshotId))]
    public WwmPlayerSnapshot Snapshot { get; set; } = null!;

    [Column("slot")]
    public int Slot { get; set; }

    [Column("skill_id")]
    public int SkillId { get; set; }

    [Column("name")]
    [MaxLength(100)]
    public string? Name { get; set; }
}
