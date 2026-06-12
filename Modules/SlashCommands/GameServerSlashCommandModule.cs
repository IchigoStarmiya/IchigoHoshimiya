using IchigoHoshimiya.Interfaces;
using JetBrains.Annotations;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace IchigoHoshimiya.Modules.SlashCommands;

public class GameServerSlashCommandModule(IGameServerService gameServerService)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    private const ulong AllowedGuildId = 1501672515058012250UL;
    private const ulong AllowedRoleId = 1502208618727211059UL;

    [SlashCommand("cobbleverse", "Start the Cobbleverse server and stop Palworld.")]
    [UsedImplicitly]
    public Task Cobbleverse() => RunAsync("Cobbleverse", "Palworld", gameServerService.StartCobbleverseAsync);

    [SlashCommand("palworld", "Start the Palworld server and stop Cobbleverse.")]
    [UsedImplicitly]
    public Task Palworld() => RunAsync("Palworld", "Cobbleverse", gameServerService.StartPalworldAsync);

    private async Task RunAsync(string startName, string stopName, Func<CancellationToken, Task> action)
    {
        if (Context.Interaction.GuildId != AllowedGuildId
            || Context.User is not GuildUser member
            || !member.RoleIds.Contains(AllowedRoleId))
        {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(new InteractionMessageProperties
                {
                    Content = "You do not have permission to use this command.",
                    Flags = MessageFlags.Ephemeral
                }));
            return;
        }

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());

        string content;
        if (!gameServerService.IsConfigured)
        {
            content = "Game server control is not configured.";
        }
        else
        {
            try
            {
                await action(CancellationToken.None);
                content = $"✅ {startName} is starting up; {stopName} has been stopped.";
            }
            catch (Exception ex)
            {
                content = $"❌ Failed to switch to {startName}: {ex.GetType().Name}: {ex.Message}";
            }
        }

        await Context.Interaction.ModifyResponseAsync(message => message.WithContent(content));
    }
}