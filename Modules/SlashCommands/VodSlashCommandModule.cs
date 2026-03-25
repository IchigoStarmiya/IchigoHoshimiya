using JetBrains.Annotations;
using NetCord.Services.ApplicationCommands;

namespace IchigoHoshimiya.Modules.SlashCommands;

[UsedImplicitly]
public class VodSlashCommandModule : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("vod", "Get the VOD link")]
    [UsedImplicitly]
    public string Vod()
    {
        return "https://docs.google.com/forms/d/e/1FAIpQLSclZMEJl0Qr_iU8sURQz5djqynf1jnhMPeYZ4Y8P1cS1AvKEA/viewform";
    }
}