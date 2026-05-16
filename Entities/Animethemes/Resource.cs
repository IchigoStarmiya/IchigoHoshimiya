using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IchigoHoshimiya.Entities.Animethemes;

[Table("resources")]
public class Resource
{
    [Key] [Column("resource_id")] public ulong ResourceId { get; set; }

    [Column("created_at", TypeName = "timestamp(6)")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp(6)")]
    public DateTime? UpdatedAt { get; set; }

    [Column("deleted_at", TypeName = "timestamp(6)")]
    public DateTime? DeletedAt { get; set; }

    [Column("site")] public int Site { get; set; }

    [Column("link")] [StringLength(255)] public string Link { get; set; } = null!;

    [Column("external_id")] public int? ExternalId { get; set; }

    [InverseProperty("Resource")]
    public virtual ICollection<Resourceable> Resourceables { get; set; } = new List<Resourceable>();
}
