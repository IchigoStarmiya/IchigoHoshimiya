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
            var properties = new MessageProperties
            {
                Embeds = [BuildEmbed(message)],
                Attachments = await DownloadAttachmentsAsync(message)
            };

            await client.SendEmbedMessageAsync(_settings.LogChannelId, properties);
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred while logging message {message.Id}: {e.Message}");
        }
    }

    private static EmbedProperties BuildEmbed(Message message)
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
        embed.Fields = [new EmbedFieldProperties { Name = "Source", Value = $"<#{message.ChannelId}>" }];
        embed.Footer = new EmbedFooterProperties { Text = $"Message ID: {message.Id}" };
        embed.Timestamp = message.CreatedAt;

        return embed;
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