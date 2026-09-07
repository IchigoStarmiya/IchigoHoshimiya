using System.Text;
using IchigoHoshimiya.Helpers;
using IchigoHoshimiya.Interfaces;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using NetCord;
using NetCord.Rest;
using NetCord.Services.Commands;

namespace IchigoHoshimiya.Modules.TextCommands;

public class OwnerCommandModule(IClient ichigoClient, IConfiguration configuration, RestClient restClient) : CommandModule<CommandContext>
{
    [Command("send")]
    [UsedImplicitly]
    public async Task SendCommand(string channelId, [CommandParameter(Remainder = true)] string text)
    {
        MessageProperties props = new ()
        {
            Embeds = [new EmbedProperties
            {
                Color = new Color(
                    (byte)short.Parse(configuration["EmbedColours:Red"]!),
                    (byte)short.Parse(configuration["EmbedColours:Green"]!),
                    (byte)short.Parse(configuration["EmbedColours:Blue"]!)),
                Description = text
            }]
        };
        await ichigoClient.SendEmbedMessageAsync(ulong.Parse(channelId), props);
    }

    [Command("edit")]
    [UsedImplicitly]
    public async Task EditCommand(string channelId, string messageId, [CommandParameter(Remainder = true)] string text)
    {
        MessageProperties props = new ()
        {
            Embeds = [EmbedHelper.Build(null, text)]
        };
        await ichigoClient.EditEmbedMessageAsync(ulong.Parse(channelId), ulong.Parse(messageId), props);
    }

    [Command("servers")]
    [UsedImplicitly]
    public async Task ListServersCommand()
    {
        var ownerUserId = configuration.GetValue<ulong>("Discord:OwnerUserId");
        if (Context.Message.Author.Id != ownerUserId)
            return;

        var guilds = await ichigoClient.GetGuildsAsync();
        if (guilds.Count == 0)
        {
            await ichigoClient.SendMessageAsync(Context.Message.ChannelId, "I'm not in any servers.");
            return;
        }

        var lines = guilds
            .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => $"**{g.Name}** — `{g.Id}`");

        foreach (var page in Paginate(lines))
        {
            await ichigoClient.SendEmbedMessageAsync(
                Context.Message.ChannelId,
                new MessageProperties { Embeds = [EmbedHelper.Build($"Servers ({guilds.Count})", page)] });
        }
    }

    private static IEnumerable<string> Paginate(IEnumerable<string> lines)
    {
        const int maxDescriptionLength = 4000;
        var page = new StringBuilder();

        foreach (var line in lines)
        {
            if (page.Length > 0 && page.Length + line.Length + 1 > maxDescriptionLength)
            {
                yield return page.ToString();
                page.Clear();
            }

            if (page.Length > 0)
                page.Append('\n');

            page.Append(line);
        }

        if (page.Length > 0)
            yield return page.ToString();
    }

    [Command("leaveserver")]
    [UsedImplicitly]
    public async Task LeaveServerCommand(ulong guildId)
    {
        var ownerUserId = configuration.GetValue<ulong>("Discord:OwnerUserId");
        if (Context.Message.Author.Id != ownerUserId)
            return;

        await restClient.LeaveGuildAsync(guildId);
    }
}