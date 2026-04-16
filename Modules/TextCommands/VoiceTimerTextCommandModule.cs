using IchigoHoshimiya.Interfaces;
using IchigoHoshimiya.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using NetCord.Rest;
using NetCord.Services.Commands;

namespace IchigoHoshimiya.Modules.TextCommands;

[UsedImplicitly]
public class VoiceTimerTextCommandModule(IVoiceTimerService voiceTimerService, IOptions<VoiceTimerSettings> options, RestClient restClient)
    : CommandModule<CommandContext>
{
    [Command("starttimer")]
    [UsedImplicitly]
    public async Task StartTimer()
    {
        var guildId = Context.Message.GuildId;
        if (guildId is null)
        {
            await ReplyAsync("This command must be used in a server.");
            return;
        }

        if (!voiceTimerService.IsConfigured(guildId.Value))
        {
            await ReplyAsync("This server is not configured for the jungle timer.");
            return;
        }

        if (!await IsAuthorizedAsync(guildId.Value))
        {
            await ReplyAsync("You do not have permission to use this command.");
            return;
        }

        try
        {
            await voiceTimerService.StartAsync(guildId.Value);
            await ReplyAsync("Timer started.");
        }
        catch (Exception ex)
        {
            await ReplyAsync($"Failed to start the timer: {ex.GetType().Name}: {ex.Message}");
        }
    }

    [Command("stoptimer")]
    [UsedImplicitly]
    public async Task StopTimer()
    {
        var guildId = Context.Message.GuildId;
        if (guildId is null)
        {
            await ReplyAsync("This command must be used in a server.");
            return;
        }

        if (!voiceTimerService.IsConfigured(guildId.Value))
        {
            await ReplyAsync("This server is not configured for the jungle timer.");
            return;
        }

        if (!await IsAuthorizedAsync(guildId.Value))
        {
            await ReplyAsync("You do not have permission to use this command.");
            return;
        }

        if (!voiceTimerService.IsRunning(guildId.Value))
        {
            await ReplyAsync("The timer is not running.");
            return;
        }

        try
        {
            await voiceTimerService.StopAsync(guildId.Value);
            await ReplyAsync("Timer stopped and bot disconnected.");
        }
        catch (Exception ex)
        {
            await ReplyAsync($"Failed to stop the timer: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private async Task<bool> IsAuthorizedAsync(ulong guildId)
    {
        var guildSettings = options.Value.Servers.FirstOrDefault(s => s.GuildId == guildId);
        if (guildSettings is null) return false;
        var member = await restClient.GetGuildUserAsync(guildId, Context.Message.Author.Id);
        return member.RoleIds.Contains(guildSettings.AuthorizedRoleId);
    }

    private Task ReplyAsync(string content) =>
        Context.Message.ReplyAsync(new ReplyMessageProperties { Content = content });
}
