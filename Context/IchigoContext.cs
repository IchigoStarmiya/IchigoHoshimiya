using System.Text.Json;
using IchigoHoshimiya.Entities.AniList;
using IchigoHoshimiya.Entities.General;
using Microsoft.EntityFrameworkCore;

namespace IchigoHoshimiya.Context;

public class IchigoContext : DbContext
{
    public IchigoContext()
    {
    }

    public IchigoContext(DbContextOptions<IchigoContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AiringAnime> AiringAnime { get; set; }

    public virtual DbSet<AiringEpisode> AiringEpisodes { get; set; }

    public virtual DbSet<RssReminder> RssReminder { get; set; }

    public virtual DbSet<GrassToucher> GrassToucher { get; set; }
    
    public virtual DbSet<TrackedTicket> TrackedTickets { get; set; }
    
    public virtual DbSet<TicketMessage> TicketMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
          base.OnModelCreating(modelBuilder);

          // Configure TrackedTicket
          modelBuilder.Entity<TrackedTicket>(entity =>
          {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.ChannelId)
                      .IsUnique();

                entity.Property(e => e.TicketName)
                      .IsRequired()
                      .HasMaxLength(100);

                // Configure one-to-many relationship
                entity.HasMany(e => e.Messages)
                      .WithOne(m => m.TrackedTicket)
                      .HasForeignKey(m => m.TrackedTicketId)
                      .OnDelete(DeleteBehavior.Cascade);
          });

          // Configure TicketMessage
          modelBuilder.Entity<TicketMessage>(entity =>
          {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.DiscordMessageId);
                entity.HasIndex(e => e.ChannelId);

                entity.HasIndex(e => new { e.ChannelId, e.DiscordMessageId })
                      .IsUnique();

                entity.Property(e => e.AuthorName)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.Content)
                      .IsRequired();

                // Configure value converter for List<string> to JSON
                entity.Property(e => e.AttachmentUrls)
                      .HasConversion(
                             v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                             v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ??
                                  new List<string>()
                       )
                      .HasColumnType("TEXT"); // Use TEXT for SQLite, NVARCHAR(MAX) for SQL Server
          });
    }
}