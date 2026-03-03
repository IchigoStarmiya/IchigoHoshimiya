using JetBrains.Annotations;
using NetCord.Services.Commands;

namespace IchigoHoshimiya.Modules.TextCommands;

[UsedImplicitly]
public class VodTextCommandModule : CommandModule<CommandContext>
{
    [Command("vod")]
    [UsedImplicitly]
    public string Vod()
    {
        return "https://docs.google.com/forms/d/e/1FAIpQLSdCAYzw2YkI8l4isLY0k4a28rln3OOMm95KD7FOuugheoo06g/viewform?usp=dialog";
    }
}