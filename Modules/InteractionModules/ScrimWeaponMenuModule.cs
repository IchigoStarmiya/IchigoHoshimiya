using IchigoHoshimiya.Entities.General;
using IchigoHoshimiya.Helpers;
using IchigoHoshimiya.Interfaces;
using JetBrains.Annotations;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace IchigoHoshimiya.Modules.InteractionModules;

[UsedImplicitly]
public class ScrimWeaponMenuModule(IScrimService scrimService) : ComponentInteractionModule<StringMenuInteractionContext>
{
    [ComponentInteraction(ScrimSignupHelper.WeaponMenuId)]
    [UsedImplicitly]
    public async Task SelectWeapon(long signupId)
    {
        var signup = await scrimService.GetSignupAsync(signupId);

        if (signup is null)
        {
            await RespondEphemeralAsync("That scrim signup no longer exists.");
            return;
        }

        if (!signup.IsOpen)
        {
            await RespondEphemeralAsync("That scrim signup is closed.");
            return;
        }

        var selectedValue = Context.SelectedValues.FirstOrDefault();

        if (!int.TryParse(selectedValue, out var weaponValue) || !ScrimSignupHelper.IsValidWeapon(weaponValue))
        {
            await RespondEphemeralAsync("That weapon selection is invalid.");
            return;
        }

        var weapon = (ScrimWeapon)weaponValue;
        var existingEntry = signup.Entries.FirstOrDefault(entry => entry.UserId == Context.User.Id);
        var selectedDays = existingEntry?.AvailableDays ?? ScrimAvailableDays.None;

        await Context.Interaction.SendResponseAsync(
            InteractionCallback.ModifyMessage(message =>
            {
                message.Content = BuildDaySelectionText(weapon, selectedDays);
                message.Components = ScrimSignupHelper.BuildDaySelectionComponents(signupId, weapon, selectedDays);
            }));
    }

    private static string BuildDaySelectionText(ScrimWeapon weapon, ScrimAvailableDays selectedDays)
    {
        var role = ScrimSignupHelper.GetRoleForWeapon(weapon);
        return $"Weapon: **{ScrimSignupHelper.FormatWeapon(weapon)}**\nRole: **{ScrimSignupHelper.FormatRole(role)}**\nAvailable days: **{ScrimSignupHelper.FormatDays(selectedDays)}**\n\nToggle the weekdays you can play, then press **Save Signup**.";
    }

    private Task RespondEphemeralAsync(string content)
    {
        return Context.Interaction.SendResponseAsync(
            InteractionCallback.Message(
                new InteractionMessageProperties
                {
                    Content = content,
                    Flags = MessageFlags.Ephemeral
                }));
    }
}
