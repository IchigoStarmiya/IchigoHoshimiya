using IchigoHoshimiya.Interfaces;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace IchigoHoshimiya.Modules.SlashCommands;

[SlashCommand("activity", "Daily activity signups")]
public class ActivitySignupSlashCommandModule(
    IActivitySignupService signupService,
    IConfiguration configuration)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("test", "Post a test signup in this channel (bot owner only)")]
    [UsedImplicitly]
    public async Task PostTestSignup(
        [SlashCommandParameter(Name = "minutes", Description = "Minutes until it closes and tallies (defaults to 2)",
            MinValue = 1, MaxValue = 1440)]
        int minutes = 2)
    {
        var ownerUserId = configuration.GetValue<ulong>("Discord:OwnerUserId");

        if (ownerUserId == 0 || Context.User.Id != ownerUserId)
        {
            await RespondAsync(Ephemeral("Only the bot owner can use this command."));
            return;
        }

        await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        var signup = await signupService.PostTestSignupAsync(Context.Channel.Id, TimeSpan.FromMinutes(minutes));
        var closesAt = new DateTimeOffset(signup.CloseAtUtc).ToUnixTimeSeconds();

        await Context.Interaction.ModifyResponseAsync(message =>
            message.Content = $"🧪 Test signup posted, it closes and tallies <t:{closesAt}:R>.");
    }

    private static InteractionCallbackProperties<InteractionMessageProperties> Ephemeral(string content)
    {
        return InteractionCallback.Message(new InteractionMessageProperties
        {
            Content = content,
            Flags = MessageFlags.Ephemeral
        });
    }
}
