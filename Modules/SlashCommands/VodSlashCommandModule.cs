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
        return "https://docs.google.com/forms/d/e/1FAIpQLSdCAYzw2YkI8l4isLY0k4a28rln3OOMm95KD7FOuugheoo06g/viewform?usp=dialog";
    }
}