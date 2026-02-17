using IchigoHoshimiya.Interfaces;
using Microsoft.Extensions.Configuration;
using NetCord;
using NetCord.Rest;

namespace IchigoHoshimiya.Adapters;

public class RestClientAdapter(RestClient restClient) : IClient
{
    public Task SendMessageAsync(ulong channelId, string content)
    {
        return restClient.SendMessageAsync(channelId, content);
    }

    public Task SendEmbedMessageAsync(ulong channelId, MessageProperties messageProperties)
    {
        return restClient.SendMessageAsync(channelId, messageProperties);
    }

    public Task EditEmbedMessageAsync(ulong channelId, ulong messageId, MessageProperties messageProperties)
    {
        return restClient.ModifyMessageAsync(
            channelId,
            messageId,
            options =>
            {
                options.Content = messageProperties.Content;
                options.Embeds = messageProperties.Embeds;
            });
    }

    public Task DeleteMessageAsync(ulong channelId, ulong messageId)
    {
        return restClient.DeleteMessageAsync(channelId, messageId);
    }

    public async Task<RestMessage> GetMessageAsync(ulong channelId, ulong messageId)
    {
        var message = await restClient.GetMessageAsync(channelId, messageId);
        return message;
    }

    public async Task AddRoleToUser(ulong guildId, ulong userId, ulong roleId)
    {
        var user = await restClient.GetGuildUserAsync(guildId, userId);
        await user.AddRoleAsync(roleId);
    }

    public async Task RemoveRoleFromUser(ulong guildId, ulong userId, ulong roleId)
    {
        var user = await restClient.GetGuildUserAsync(guildId, userId);
        await user.RemoveRoleAsync(roleId);
    }

    public async Task<IEnumerable<TextGuildChannel>> GetChannelsInCategoriesAsync(ulong guildId, IEnumerable<ulong> categoryIds)
    {
        var targetCategories = categoryIds.ToHashSet();
        var result = new List<TextGuildChannel>();
        
        var channels = await restClient.GetGuildChannelsAsync(guildId);
        
        foreach (var channel in channels)
        {
            if (channel is TextGuildChannel textChannel && 
                textChannel.ParentId.HasValue && 
                targetCategories.Contains(textChannel.ParentId.Value))
            {
                result.Add(textChannel);
            }
        }

        return result;
    }

    public async Task<IEnumerable<RestMessage>> GetMessagesAfterIdAsync(ulong channelId, ulong afterMessageId)
    {
        var messages = new List<RestMessage>();
        
        var paginationProperties = new PaginationProperties<ulong>
        {
            From = afterMessageId > 0 ? afterMessageId : null,
            Direction = PaginationDirection.After,
            BatchSize = 100
        };

        var messageEnumerable = restClient.GetMessagesAsync(channelId, paginationProperties);

        await foreach (var message in messageEnumerable)
        {
            messages.Add(message);
        }
        
        return messages.OrderBy(m => m.Id);
    }

    public async Task SendFileAsync(ulong channelId, Stream fileStream, string fileName, string messageContent)
    {
        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }
        
        var attachment = new AttachmentProperties(fileName, fileStream);

        await restClient.SendMessageAsync(channelId, new MessageProperties
        {
            Content = messageContent,
            Attachments = [attachment]
        });
    }
}