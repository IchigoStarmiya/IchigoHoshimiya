using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IchigoHoshimiya.Entities.General;

public class TrackedTicket
{
    [Key]
    public int Id { get; set; }
    
    public ulong ChannelId { get; set; }
    
    [MaxLength(100)]
    public string TicketName { get; set; } = string.Empty;
    
    public bool IsClosed { get; set; }
    
    public DateTimeOffset LastSyncedAt { get; set; }
    
    public List<TicketMessage> Messages { get; set; } = new();
}

public class TicketMessage
{
    [Key]
    public long Id { get; set; }
    
    public ulong DiscordMessageId { get; set; }
    
    public ulong ChannelId { get; set; }
    
    public int TrackedTicketId { get; set; }
    
    [ForeignKey(nameof(TrackedTicketId))]
    public TrackedTicket TrackedTicket { get; set; } = null!;
    
    public ulong AuthorId { get; set; }
    
    [MaxLength(200)]
    public string AuthorName { get; set; } = string.Empty;
    
    public string Content { get; set; } = string.Empty;
    
    public DateTimeOffset Timestamp { get; set; }
    
    // This will be configured to serialize/deserialize JSON automatically
    public List<string> AttachmentUrls { get; set; } = new();
}