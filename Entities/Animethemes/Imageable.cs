using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace IchigoHoshimiya.Entities.Animethemes;

[Table("imageables")]
[Index("ImageId", "ImageableType", "ImageableId", Name = "imageables_unique_index", IsUnique = true)]
[Index("ImageableType", "ImageableId", Name = "imageables_imageable_type_imageable_id_index")]
public class Imageable
{
    [Key] [Column("id")] public ulong Id { get; set; }

    [Column("image_id")] public ulong ImageId { get; set; }

    [Column("imageable_type")]
    [StringLength(255)]
    public string ImageableType { get; set; } = null!;

    [Column("imageable_id")] public ulong ImageableId { get; set; }

    [Column("depth")] public int Depth { get; set; }

    [Column("created_at", TypeName = "timestamp(6)")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp(6)")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("ImageId")]
    [InverseProperty("Imageables")]
    public virtual Image Image { get; set; } = null!;
}
