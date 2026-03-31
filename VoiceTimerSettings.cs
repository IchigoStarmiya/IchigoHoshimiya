namespace IchigoHoshimiya.Services;

public class VoiceTimerSettings
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public string StartClipPath { get; set; } = "Audio/jungle.ogg";
    public string FourMinClipPath { get; set; } = "Audio/jungle.ogg";
    public string FiveMinClipPath { get; set; } = "Audio/jungle.ogg";
    public string FfmpegPath { get; set; } = "ffmpeg";
}
