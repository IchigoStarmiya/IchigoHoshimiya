using IchigoHoshimiya.Interfaces;
using JetBrains.Annotations;
using NetCord.Rest;
using NetCord.Services.Commands;

namespace IchigoHoshimiya.Modules.TextCommands;

[UsedImplicitly]
public class HelpTextCommandModule(IHelpService helpService)
    : CommandModule<CommandContext>
{
    [Command("help")]
    [UsedImplicitly]
    public async Task Help()
    {
        var embed = helpService.GetHelpEmbed();

        await Context.Message.SendAsync(new MessageProperties().WithEmbeds([embed]));
    }
}
