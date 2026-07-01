using System.Collections.Concurrent;
using IchigoHoshimiya.Helpers;
using IchigoHoshimiya.Interfaces;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Rest;

namespace IchigoHoshimiya.Handlers;

public class MessageLoggerSettings
{
    public List<ulong> SourceChannelIds { get; set; } = [];
    public ulong LogChannelId { get; set; }
}

[UsedImplicitly]
public class MessageLoggerHandler(IClient client, IOptions<MessageLoggerSettings> options)
    : IMessageCreateGatewayHandler
{
    private static readonly HttpClient SHttpClient = new();

    private static readonly ConcurrentDictionary<ulong, string> SGuildNameCache = new();
    private static readonly ConcurrentDictionary<ulong, string> SChannelNameCache = new();

    private readonly MessageLoggerSettings _settings = options.Value;

    public ValueTask HandleAsync(Message message)
    {
        if (_settings.LogChannelId == 0 || !_settings.SourceChannelIds.Contains(message.ChannelId))
        {
            return ValueTask.CompletedTask;
        }
        
        if (message.ChannelId == _settings.LogChannelId)
        {
            return ValueTask.CompletedTask;
        }

        LogMessage(message);

        return ValueTask.CompletedTask;
    }

    private async void LogMessage(Message message)
    {
        try
        {
            var sourceLabel = await BuildSourceLabelAsync(message);

            var properties = new MessageProperties
            {
                Embeds = [BuildEmbed(message, sourceLabel)],
                Attachments = await DownloadAttachmentsAsync(message)
            };

            await client.SendEmbedMessageAsync(_settings.LogChannelId, properties);
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred while logging message {message.Id}: {e.Message}");
        }
    }

    private static EmbedProperties BuildEmbed(Message message, string sourceLabel)
    {
        var description = string.IsNullOrWhiteSpace(message.Content)
            ? "*(no text content)*"
            : message.Content;

        var embed = EmbedHelper.Build(description: description);

        embed.Author = new EmbedAuthorProperties
        {
            Name = $"{message.Author.Username} ({message.Author.Id})",
            IconUrl = message.Author.GetAvatarUrl()?.ToString()
        };
        embed.Fields = [new EmbedFieldProperties { Name = "Source", Value = sourceLabel }];
        embed.Footer = new EmbedFooterProperties { Text = $"Message ID: {message.Id}" };
        embed.Timestamp = message.CreatedAt;

        return embed;
    }

    private async Task<string> BuildSourceLabelAsync(Message message)
    {
        var channelName = await GetCachedNameAsync(
            SChannelNameCache, message.ChannelId, () => client.GetChannelNameAsync(message.ChannelId));

        var channelLabel = channelName is null ? $"#{message.ChannelId}" : $"#{channelName}";

        if (message.GuildId is not { } guildId)
        {
            return channelLabel;
        }

        var guildName = await GetCachedNameAsync(
            SGuildNameCache, guildId, () => client.GetGuildNameAsync(guildId));

        return guildName is null ? channelLabel : $"{guildName} / {channelLabel}";
    }

    private static async Task<string?> GetCachedNameAsync(
        ConcurrentDictionary<ulong, string> cache, ulong id, Func<Task<string?>> fetch)
    {
        if (cache.TryGetValue(id, out var cached))
        {
            return cached;
        }

        var name = await fetch();

        if (name is not null)
        {
            cache[id] = name;
        }

        return name;
    }

    private static async Task<List<AttachmentProperties>> DownloadAttachmentsAsync(Message message)
    {
        var attachments = new List<AttachmentProperties>();

        foreach (var attachment in message.Attachments)
        {
            try
            {
                var memoryStream = new MemoryStream();
                await using var sourceStream = await SHttpClient.GetStreamAsync(attachment.Url);
                await sourceStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                attachments.Add(new AttachmentProperties(attachment.FileName, memoryStream));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to mirror attachment {attachment.FileName}: {e.Message}");
            }
        }

        return attachments;
    }
}