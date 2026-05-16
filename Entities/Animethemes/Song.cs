using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IchigoHoshimiya.Entities.Animethemes;

[Table("songs")]
public class Song
{
    [Key] [Column("song_id")] public ulong SongId { get; set; }

    [Column("created_at", TypeName = "timestamp(6)")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp(6)")]
    public DateTime? UpdatedAt { get; set; }

    [Column("deleted_at", TypeName = "timestamp(6)")]
    public DateTime? DeletedAt { get; set; }

    [Column("title")] [StringLength(255)] public string? Title { get; set; }

    [Column("title_native")]
    [StringLength(255)]
    public string? TitleNative { get; set; }

    [InverseProperty("Song")] public virtual ICollection<AnimeTheme> AnimeThemes { get; set; } = new List<AnimeTheme>();

    [InverseProperty("Song")]
    public virtual ICollection<Performance> Performances { get; set; } = new List<Performance>();
}
