using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace IchigoHoshimiya.Entities.Animethemes;

[Table("anime_theme_entries")]
[Index("ThemeId", Name = "anime_theme_entries_theme_id_foreign")]
public class AnimeThemeEntry
{
    [Key] [Column("entry_id")] public ulong EntryId { get; set; }

    [Column("created_at", TypeName = "timestamp(6)")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp(6)")]
    public DateTime? UpdatedAt { get; set; }

    [Column("deleted_at", TypeName = "timestamp(6)")]
    public DateTime? DeletedAt { get; set; }

    [Column("version")] public int Version { get; set; }

    [Column("episodes")]
    [StringLength(255)]
    public string? Episodes { get; set; }

    [Column("nsfw")] public bool Nsfw { get; set; }

    [Column("spoiler")] public bool Spoiler { get; set; }

    [Column("notes", TypeName = "text")] public string? Notes { get; set; }

    [Column("theme_id")] public ulong ThemeId { get; set; }

    [InverseProperty("Entry")]
    public virtual ICollection<AnimeThemeEntryVideo> AnimeThemeEntryVideos { get; set; } =
        new List<AnimeThemeEntryVideo>();

    [ForeignKey("ThemeId")]
    [InverseProperty("AnimeThemeEntries")]
    public virtual AnimeTheme Theme { get; set; } = null!;
}