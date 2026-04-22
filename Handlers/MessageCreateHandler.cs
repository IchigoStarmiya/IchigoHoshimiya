using IchigoHoshimiya.Interfaces;
using JetBrains.Annotations;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace IchigoHoshimiya.Handlers;

[UsedImplicitly]
public class MessageCreateHandler(IClient client, ITwitterReplacementService twitterReplacementService)
    : IMessageCreateGatewayHandler
{
    private static readonly HashSet<ulong> SAllowedGuildIds = [549910122600857610, 514203145333899276];

    public ValueTask HandleAsync(Message message)
    {
        if (message.Author.IsBot)
        {
            return ValueTask.CompletedTask;
        }

        if (message.GuildId is not { } guildId || !SAllowedGuildIds.Contains(guildId))
        {
            return ValueTask.CompletedTask;
        }

        HandleTwitter(message, message.Author.Username);

        return ValueTask.CompletedTask;
    }

    private async void HandleTwitter(Message message, string username)
    {
        try
        {
            var newContent = await twitterReplacementService.GetReplacedContentAsync(message.Content, username);

            if (newContent is null)
            {
                return;
            }

            _ = client.SendMessageAsync(message.ChannelId, newContent);
            _ = client.DeleteMessageAsync(message.ChannelId, message.Id);
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred while sending the message: {e.Message}");
        }
    }
}