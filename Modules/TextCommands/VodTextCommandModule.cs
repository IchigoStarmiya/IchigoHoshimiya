using JetBrains.Annotations;
using NetCord.Services.Commands;

namespace IchigoHoshimiya.Modules.TextCommands;

[UsedImplicitly]
public class VodTextCommandModule : CommandModule<CommandContext>
{
    [Command("vod")]
    [UsedImplicitly]
    public static string Vod()
    {
        return "https://docs.google.com/forms/d/e/1FAIpQLSclZMEJl0Qr_iU8sURQz5djqynf1jnhMPeYZ4Y8P1cS1AvKEA/viewform";
    }
}