using IchigoHoshimiya.Interfaces;
using JetBrains.Annotations;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace IchigoHoshimiya.Modules.SlashCommands;

[UsedImplicitly]
public class HelpSlashCommandModule(IHelpService helpService)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("help", "Show all bot commands and their descriptions.")]
    [UsedImplicitly]
    public async Task Help()
    {
        await RespondAsync(InteractionCallback.DeferredMessage());

        var embed = helpService.GetHelpEmbed();

        await Context.Interaction.ModifyResponseAsync(m => m.Embeds = [embed]);
    }
}
