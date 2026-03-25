using JetBrains.Annotations;
using NetCord.Services.Commands;

namespace IchigoHoshimiya.Modules.TextCommands;

[UsedImplicitly]
public class TeamspeakTextCommandModule : CommandModule<CommandContext>
{
    [Command("ts")]
    [UsedImplicitly]
    public static string Ts()
    {
        return "IP: 85.215.214.174\nPassword: maister";
    }
}