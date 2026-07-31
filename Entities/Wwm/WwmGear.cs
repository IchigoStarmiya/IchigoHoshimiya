using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace IchigoHoshimiya.Entities.Wwm;

[Table("wwm_gear")]
[Index(nameof(SnapshotId), nameof(SlotOrd), IsUnique = true)]
[Index(nameof(ItemNo))]
[Index(nameof(SuitName))]
public class WwmGear
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("snapshot_id")]
    public long SnapshotId { get; set; }

    [ForeignKey(nameof(SnapshotId))]
    public WwmPlayerSnapshot Snapshot { get; set; } = null!;

    [Column("slot_name")]
    [MaxLength(64)]
    public string? SlotName { get; set; }

    [Column("slot_ord")]
    public int SlotOrd { get; set; }

    [Column("item_no")]
    public long ItemNo { get; set; }

    [Column("item_index")]
    public int? ItemIndex { get; set; }

    [Column("durability")]
    public int? Durability { get; set; }

    [Column("suit")]
    public int? Suit { get; set; }

    [Column("suit_name")]
    [MaxLength(100)]
    public string? SuitName { get; set; }

    [Column("retoned")]
    public bool Retoned { get; set; }

    [Column("retone_no")]
    public long? RetoneNo { get; set; }

    [Column("retone_at_utc")]
    public DateTime? RetoneAtUtc { get; set; }

    [Column("retone_count")]
    public int? RetoneCount { get; set; }

    public List<WwmGearRetone> RetoneHistory { get; set; } = [];

    public List<WwmGearBaseAttr> BaseAttrs { get; set; } = [];

    public List<WwmGearAffix> Affixes { get; set; } = [];
}

[Table("wwm_gear_retone")]
[Index(nameof(GearId), nameof(Ordinal), IsUnique = true)]
public class WwmGearRetone
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("gear_id")]
    public long GearId { get; set; }

    [ForeignKey(nameof(GearId))]
    public WwmGear Gear { get; set; } = null!;

    [Column("ordinal")]
    public int Ordinal { get; set; }

    [Column("retone_no")]
    public long RetoneNo { get; set; }
}

[Table("wwm_gear_base_attr")]
[Index(nameof(GearId), nameof(Ordinal), IsUnique = true)]
[Index(nameof(RawName))]
public class WwmGearBaseAttr
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("gear_id")]
    public long GearId { get; set; }

    [ForeignKey(nameof(GearId))]
    public WwmGear Gear { get; set; } = null!;

    [Column("ordinal")]
    public int Ordinal { get; set; }

    [Column("raw_name")]
    [MaxLength(64)]
    public string? RawName { get; set; }

    [Column("name")]
    [MaxLength(128)]
    public string? Name { get; set; }

    [Column("value")]
    public double Value { get; set; }
}

[Table("wwm_gear_affix")]
[Index(nameof(GearId), nameof(Ordinal), IsUnique = true)]
[Index(nameof(AffixId))]
[Index(nameof(Type))]
public class WwmGearAffix
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("gear_id")]
    public long GearId { get; set; }

    [ForeignKey(nameof(GearId))]
    public WwmGear Gear { get; set; } = null!;

    [Column("ordinal")]
    public int Ordinal { get; set; }

    [Column("affix_id")]
    public long AffixId { get; set; }

    [Column("name")]
    [MaxLength(1000)]
    public string? Name { get; set; }

    [Column("value")]
    public double Value { get; set; }

    [Column("type")]
    [MaxLength(32)]
    public string? Type { get; set; }

    [Column("active")]
    public bool? Active { get; set; }

    [Column("display_value")]
    [MaxLength(64)]
    public string? DisplayValue { get; set; }
}
