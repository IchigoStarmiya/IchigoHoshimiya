using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace IchigoHoshimiya.Entities.Animethemes;

[Table("anime_themes")]
[Index("AnimeId", Name = "anime_themes_anime_id_foreign")]
[Index("GroupId", Name = "anime_themes_group_id_foreign")]
[Index("SongId", Name = "anime_themes_song_id_foreign")]
public class AnimeTheme
{
    [Key] [Column("theme_id")] public ulong ThemeId { get; set; }

    [Column("created_at", TypeName = "timestamp(6)")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp(6)")]
    public DateTime? UpdatedAt { get; set; }

    [Column("deleted_at", TypeName = "timestamp(6)")]
    public DateTime? DeletedAt { get; set; }

    [Column("type")] public int Type { get; set; }

    [Column("sequence")] public int? Sequence { get; set; }

    [Column("slug")] [StringLength(255)] public string Slug { get; set; } = null!;

    [Column("anime_id")] public ulong AnimeId { get; set; }

    [Column("song_id")] public ulong? SongId { get; set; }

    [Column("group_id")] public ulong? GroupId { get; set; }

    [ForeignKey("AnimeId")]
    [InverseProperty("AnimeThemes")]
    public virtual Anime Anime { get; set; } = null!;

    [InverseProperty("Theme")]
    public virtual ICollection<AnimeThemeEntry> AnimeThemeEntries { get; set; } = new List<AnimeThemeEntry>();

    [ForeignKey("GroupId")]
    [InverseProperty("AnimeThemes")]
    public virtual Group? Group { get; set; }

    [ForeignKey("SongId")]
    [InverseProperty("AnimeThemes")]
    public virtual Song? Song { get; set; }
}