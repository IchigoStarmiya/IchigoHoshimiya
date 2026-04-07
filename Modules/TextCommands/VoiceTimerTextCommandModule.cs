using IchigoHoshimiya.Interfaces;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using NetCord.Rest;
using NetCord.Services.Commands;

namespace IchigoHoshimiya.Modules.TextCommands;

[UsedImplicitly]
public class VoiceTimerTextCommandModule(IVoiceTimerService voiceTimerService, IMaiJungleService maiJungleService, IConfiguration configuration, RestClient restClient)
    : CommandModule<CommandContext>
{
    [Command("starttimer")]
    [UsedImplicitly]
    public async Task StartTimer()
    {
        if (!await IsCouncilMemberAsync())
        {
            await ReplyAsync("You do not have permission to use this command.");
            return;
        }

        try
        {
            await voiceTimerService.StartAsync();
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
        if (!await IsCouncilMemberAsync())
        {
            await ReplyAsync("You do not have permission to use this command.");
            return;
        }

        if (!voiceTimerService.IsRunning)
        {
            await ReplyAsync("The timer is not running.");
            return;
        }

        try
        {
            await voiceTimerService.StopAsync();
            await ReplyAsync("Timer stopped and bot disconnected.");
        }
        catch (Exception ex)
        {
            await ReplyAsync($"Failed to stop the timer: {ex.GetType().Name}: {ex.Message}");
        }
    }

    [Command("maijungle")]
    [UsedImplicitly]
    public async Task MaiJungle()
    {
        if (!await IsCouncilMemberAsync())
        {
            await ReplyAsync("You do not have permission to use this command.");
            return;
        }

        try
        {
            await maiJungleService.StartAsync();
            await ReplyAsync("MaiJungle started.");
        }
        catch (Exception ex)
        {
            await ReplyAsync($"Failed to start MaiJungle: {ex.GetType().Name}: {ex.Message}");
        }
    }

    [Command("stopmaijungle")]
    [UsedImplicitly]
    public async Task StopMaiJungle()
    {
        if (!await IsCouncilMemberAsync())
        {
            await ReplyAsync("You do not have permission to use this command.");
            return;
        }

        if (!maiJungleService.IsRunning)
        {
            await ReplyAsync("MaiJungle is not running.");
            return;
        }

        try
        {
            await maiJungleService.StopAsync();
            await ReplyAsync("MaiJungle stopped and bot disconnected.");
        }
        catch (Exception ex)
        {
            await ReplyAsync($"Failed to stop MaiJungle: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private async Task<bool> IsCouncilMemberAsync()
    {
        var guildId = Context.Message.GuildId;
        if (guildId is null) return false;
        var councilRoles = configuration.GetSection("CouncilRole").Get<ulong[]>() ?? [];
        var member = await restClient.GetGuildUserAsync(guildId.Value, Context.Message.Author.Id);
        return member.RoleIds.Any(r => councilRoles.Contains(r));
    }

    private Task ReplyAsync(string content) =>
        Context.Message.ReplyAsync(new ReplyMessageProperties { Content = content });
}