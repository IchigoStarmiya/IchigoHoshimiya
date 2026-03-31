using IchigoHoshimiya.Interfaces;
using JetBrains.Annotations;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace IchigoHoshimiya.Modules.SlashCommands;

[UsedImplicitly]
public class VoiceTimerSlashCommandModule(IVoiceTimerService voiceTimerService)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("starttimer", "Start (or reset) the voice channel timer")]
    [UsedImplicitly]
    public async Task StartTimer()
    {
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        try
        {
            await voiceTimerService.StartAsync();
            await Context.Interaction.ModifyResponseAsync(m => m.WithContent("Timer started."));
        }
        catch (Exception ex)
        {
            await Context.Interaction.ModifyResponseAsync(m =>
                m.WithContent($"Failed to start the timer: {ex.GetType().Name}: {ex.Message}"));
        }
    }

    [SlashCommand("stoptimer", "Stop the voice channel timer and disconnect the bot")]
    [UsedImplicitly]
    public async Task StopTimer()
    {
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        if (!voiceTimerService.IsRunning)
        {
            await Context.Interaction.ModifyResponseAsync(m => m.WithContent("The timer is not running."));
            return;
        }

        try
        {
            await voiceTimerService.StopAsync();
            await Context.Interaction.ModifyResponseAsync(m => m.WithContent("Timer stopped and bot disconnected."));
        }
        catch (Exception ex)
        {
            await Context.Interaction.ModifyResponseAsync(m =>
                m.WithContent($"Failed to stop the timer: {ex.GetType().Name}: {ex.Message}"));
        }
    }
}
