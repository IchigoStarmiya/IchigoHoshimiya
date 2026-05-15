using IchigoHoshimiya.Interfaces;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using NetCord.Services.ApplicationCommands;

namespace IchigoHoshimiya.Modules.SlashCommands;

public class HetznerSlashCommandModule(IHetznerService hetznerService, IConfiguration configuration)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("startvps", "Power on the configured Hetzner VPS.")]
    [UsedImplicitly]
    public Task<string> StartVps() => RunAsync(start: true);

    [SlashCommand("stopvps", "Gracefully shut down the configured Hetzner VPS.")]
    [UsedImplicitly]
    public Task<string> StopVps() => RunAsync(start: false);

    private async Task<string> RunAsync(bool start)
    {
        var ownerUserId = configuration.GetValue<ulong>("Discord:OwnerUserId");
        if (Context.User.Id != ownerUserId)
            return "You do not have permission to use this command.";

        if (!hetznerService.IsConfigured)
            return "Hetzner is not configured.";

        var verb = start ? "start" : "stop";
        try
        {
            if (start)
                await hetznerService.StartServerAsync();
            else
                await hetznerService.StopServerAsync();
            return $"VPS {verb} requested.";
        }
        catch (Exception ex)
        {
            return $"Failed to {verb} the VPS: {ex.GetType().Name}: {ex.Message}";
        }
    }
}
