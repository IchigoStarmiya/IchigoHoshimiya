using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace IchigoHoshimiya.Entities.Animethemes;

[Table("performances")]
[Index("ArtistId", Name = "performances_artist_type_artist_id_index")]
[Index("MemberId", Name = "performances_member_id_foreign")]
[Index("SongId", "ArtistId", "MemberId", "DeletedAt", Name = "unique_performance", IsUnique = true)]
public class Performance
{
    [Key] [Column("performance_id")] public ulong PerformanceId { get; set; }

    [Column("song_id")] public ulong SongId { get; set; }

    [Column("artist_id")] public ulong ArtistId { get; set; }

    [Column("member_id")] public ulong? MemberId { get; set; }

    [Column("alias")] [StringLength(255)] public string? Alias { get; set; }

    [Column("as")] [StringLength(255)] public string? As { get; set; }

    [Column("member_alias")]
    [StringLength(255)]
    public string? MemberAlias { get; set; }

    [Column("member_as")] [StringLength(255)] public string? MemberAs { get; set; }

    [Column("relevance")] public int Relevance { get; set; }

    [Column("created_at", TypeName = "timestamp(6)")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp(6)")]
    public DateTime? UpdatedAt { get; set; }

    [Column("deleted_at", TypeName = "timestamp(6)")]
    public DateTime? DeletedAt { get; set; }

    [ForeignKey("SongId")]
    [InverseProperty("Performances")]
    public virtual Song Song { get; set; } = null!;

    [ForeignKey("ArtistId")]
    [InverseProperty("PerformanceArtists")]
    public virtual Artist Artist { get; set; } = null!;

    [ForeignKey("MemberId")]
    [InverseProperty("PerformanceMembers")]
    public virtual Artist? Member { get; set; }
}
