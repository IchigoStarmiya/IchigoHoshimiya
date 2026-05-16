using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace IchigoHoshimiya.Entities.Animethemes;

[Table("synonyms")]
[Index("SynonymableType", "SynonymableId", Name = "synonyms_synonymable_type_synonymable_id_index")]
public class Synonym
{
    [Key] [Column("synonym_id")] public ulong SynonymId { get; set; }

    [Column("synonymable_type")]
    [StringLength(255)]
    public string SynonymableType { get; set; } = null!;

    [Column("synonymable_id")] public ulong SynonymableId { get; set; }

    [Column("text")] [StringLength(255)] public string Text { get; set; } = null!;

    [Column("type")] public int Type { get; set; }

    [Column("created_at", TypeName = "timestamp(6)")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp(6)")]
    public DateTime? UpdatedAt { get; set; }

    [Column("deleted_at", TypeName = "timestamp(6)")]
    public DateTime? DeletedAt { get; set; }
}
