using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace IchigoHoshimiya.Entities.Animethemes;

[PrimaryKey("ArtistId", "MemberId")]
[Table("artist_member")]
[Index("MemberId", Name = "artist_member_member_id_foreign")]
public class ArtistMember
{
    [Column("created_at", TypeName = "timestamp(6)")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp(6)")]
    public DateTime? UpdatedAt { get; set; }

    [Key] [Column("artist_id")] public ulong ArtistId { get; set; }

    [Key] [Column("member_id")] public ulong MemberId { get; set; }

    [Column("as")] [StringLength(255)] public string? As { get; set; }

    [Column("alias")] [StringLength(255)] public string? Alias { get; set; }

    [Column("notes")] [StringLength(255)] public string? Notes { get; set; }

    [Column("relevance")] public int Relevance { get; set; }

    [ForeignKey("ArtistId")]
    [InverseProperty("ArtistMemberArtists")]
    public virtual Artist Artist { get; set; } = null!;

    [ForeignKey("MemberId")]
    [InverseProperty("ArtistMemberMembers")]
    public virtual Artist Member { get; set; } = null!;
}
