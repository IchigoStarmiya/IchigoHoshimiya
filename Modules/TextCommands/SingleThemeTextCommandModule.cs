using IchigoHoshimiya.Interfaces;
using JetBrains.Annotations;
using NetCord.Services.Commands;

namespace IchigoHoshimiya.Modules.TextCommands;

[UsedImplicitly]
public class SingleThemeTextCommandModule(IAnimethemeService animethemeService) : CommandModule<CommandContext>
{
    [Command("theme")]
    [UsedImplicitly]
    public Task<string> GetAnimetheme([CommandParameter(Remainder = true)] string query)
    {
        return animethemeService.GetAnimetheme(query, null);
    }
}