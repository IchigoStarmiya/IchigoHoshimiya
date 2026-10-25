using IchigoHoshimiya.Entities.General;
using IchigoHoshimiya.Helpers;
using IchigoHoshimiya.Interfaces;
using JetBrains.Annotations;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace IchigoHoshimiya.Modules.InteractionModules;

[UsedImplicitly]
public class ActivitySignupButtonModule(
    IActivitySignupService signupService,
    IActivitySignupMessageBuilder messageBuilder)
    : ComponentInteractionModule<ButtonInteractionContext>
{
    [ComponentInteraction(ActivitySignupMessageBuilder.ChoiceButtonId)]
    [UsedImplicitly]
    public async Task HandleChoice(long signupId, int choiceValue)
    {
        if (!Enum.IsDefined(typeof(ActivityChoice), choiceValue))
        {
            await RespondAsync(Ephemeral("That option doesn't exist anymore."));
            return;
        }

        var result = await signupService.ToggleChoiceAsync(signupId, Context.User.Id, (ActivityChoice)choiceValue);

        switch (result.Outcome)
        {
            case ActivityChoiceOutcome.NotFound:
                await RespondAsync(Ephemeral("This signup no longer exists."));
                return;
            case ActivityChoiceOutcome.Closed:
                await RespondAsync(Ephemeral("Signups for this slot are closed."));
                return;
        }

        var signup = result.Signup!;

        await RespondAsync(InteractionCallback.ModifyMessage(message =>
        {
            message.Embeds = [messageBuilder.BuildSignupEmbed(signup)];
            message.Components = messageBuilder.BuildComponents(signup);
        }));
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
