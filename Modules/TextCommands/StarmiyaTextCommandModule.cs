using JetBrains.Annotations;
using NetCord.Services.Commands;

namespace IchigoHoshimiya.Modules.TextCommands;

[UsedImplicitly]
public class StarmiyaTextCommandModule : CommandModule<CommandContext>
{
    [Command("starmiya")]
    [UsedImplicitly]
    public static string Starmiya()
    {
        return
            "https://cdn.discordapp.com/attachments/1476945098984919040/1478876941963759657/image.png?ex=69a9fefb&is=69a8ad7b&hm=2420ab6161875a2daeea15c9781df55e6c505ff6993c817926f3f8e7391991b7&";
    }
}