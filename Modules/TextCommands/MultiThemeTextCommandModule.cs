using IchigoHoshimiya.Interfaces;
using JetBrains.Annotations;
using NetCord.Rest;
using NetCord.Services.Commands;

namespace IchigoHoshimiya.Modules.TextCommands;

[UsedImplicitly]
public class MultiThemeTextCommandModule(IAnimethemeService animethemeService)
    : CommandModule<CommandContext>
{
    [Command("themes")]
    [UsedImplicitly]
    public async Task<MessageProperties> Themes([CommandParameter(Remainder = true)] string query)
    {
        EmbedProperties embed = await animethemeService.GetAllAnimethemes(query, null);

        MessageProperties props = new MessageProperties()
           .WithEmbeds([embed]);

        return props;
    }
}