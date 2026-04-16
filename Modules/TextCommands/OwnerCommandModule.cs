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