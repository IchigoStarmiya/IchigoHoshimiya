using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace IchigoHoshimiya.Entities.Animethemes;

[Table("resourceables")]
[Index("ResourceId", "ResourceableType", "ResourceableId", Name = "resourceables_unique_index", IsUnique = true)]
[Index("ResourceableType", "ResourceableId", Name = "resourceables_resourceable_type_resourceable_id_index")]
public class Resourceable
{
    [Key] [Column("id")] public ulong Id { get; set; }

    [Column("resource_id")] public ulong ResourceId { get; set; }

    [Column("resourceable_type")]
    [StringLength(255)]
    public string ResourceableType { get; set; } = null!;

    [Column("resourceable_id")] public ulong ResourceableId { get; set; }

    [Column("as")] [StringLength(255)] public string? As { get; set; }

    [Column("created_at", TypeName = "timestamp(6)")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp(6)")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("ResourceId")]
    [InverseProperty("Resourceables")]
    public virtual Resource Resource { get; set; } = null!;
}
