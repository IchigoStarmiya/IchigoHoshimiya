using JetBrains.Annotations;
using NetCord.Services.ApplicationCommands;

namespace IchigoHoshimiya.Modules.SlashCommands;

[UsedImplicitly]
public class TeamspeakSlashCommandModule : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("ts", "Get the SHIRANUI TS IP and password")]
    [UsedImplicitly]
    public string Ts()
    {
        return "IP: 85.215.214.174\nPassword: maister";
    }
}