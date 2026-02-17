using NetCord;
using NetCord.Rest;

namespace IchigoHoshimiya.Interfaces;

public interface IClient
{
    Task SendMessageAsync(ulong channelId, string content);
    
    Task SendEmbedMessageAsync(ulong channelId, MessageProperties messageProperties);
    
    Task EditEmbedMessageAsync(ulong channelId, ulong messageId, MessageProperties messageProperties);
    
    Task DeleteMessageAsync(ulong channelId, ulong messageId);
    
    Task<RestMessage> GetMessageAsync(ulong channelId, ulong messageId);
    
    Task AddRoleToUser(ulong guildId, ulong userId, ulong roleId);
    
    Task RemoveRoleFromUser(ulong guildId, ulong userId, ulong roleId);
    
    Task<IEnumerable<TextGuildChannel>> GetChannelsInCategoriesAsync(ulong guildId, IEnumerable<ulong> categoryIds);
    
    Task<IEnumerable<RestMessage>> GetMessagesAfterIdAsync(ulong channelId, ulong afterMessageId);
    
    Task SendFileAsync(ulong channelId, Stream fileStream, string fileName, string messageContent);
}