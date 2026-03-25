using IchigoHoshimiya.Entities.General;
using IchigoHoshimiya.Helpers;
using IchigoHoshimiya.Interfaces;
using JetBrains.Annotations;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace IchigoHoshimiya.Modules.InteractionModules;

[UsedImplicitly]
public class ScrimButtonModule(IScrimService scrimService) : ComponentInteractionModule<ButtonInteractionContext>
{
    [ComponentInteraction(ScrimSignupHelper.PublicSignupButtonId)]
    [UsedImplicitly]
    public async Task OpenSignup(long signupId)
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

        var existingEntry = signup.Entries.FirstOrDefault(entry => entry.UserId == Context.User.Id);

        await Context.Interaction.SendResponseAsync(
            InteractionCallback.Message(
                new InteractionMessageProperties
                {
                    Content = BuildWeaponSelectionText(existingEntry),
                    Components = ScrimSignupHelper.BuildWeaponSelectionComponents(signupId, existingEntry?.Weapon),
                    Flags = MessageFlags.Ephemeral
                }));
    }

    [ComponentInteraction(ScrimSignupHelper.WithdrawButtonId)]
    [UsedImplicitly]
    public async Task Withdraw(long signupId)
    {
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        var signup = await scrimService.GetSignupAsync(signupId);

        if (signup is null)
        {
            await Context.Interaction.ModifyResponseAsync(message =>
                message.WithContent("That scrim signup no longer exists."));
            return;
        }

        if (!signup.IsOpen)
        {
            await Context.Interaction.ModifyResponseAsync(message =>
                message.WithContent("That scrim signup is closed."));
            return;
        }

        var removed = await scrimService.RemoveSignupEntryAsync(signupId, Context.User.Id);

        if (removed)
        {
            await scrimService.RefreshSignupMessageAsync(signupId);
        }

        await Context.Interaction.ModifyResponseAsync(message =>
            message.WithContent(removed
                ? "✅ Your scrim signup was removed."
                : "You do not currently have a signup on this scrim."));
    }

    [ComponentInteraction(ScrimSignupHelper.DayToggleButtonId)]
    [UsedImplicitly]
    public async Task ToggleDay(long signupId, int weaponValue, int selectedDaysValue, int dayValue)
    {
        if (!ScrimSignupHelper.IsValidWeapon(weaponValue) ||
            !ScrimSignupHelper.IsValidDaySelection(selectedDaysValue) ||
            !ScrimSignupHelper.IsValidDaySelection(dayValue))
        {
            await RespondEphemeralAsync("That day selection is invalid.");
            return;
        }

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

        var weapon = (ScrimWeapon)weaponValue;
        var selectedDays = (ScrimAvailableDays)selectedDaysValue;
        var toggledDay = (ScrimAvailableDays)dayValue;
        var updatedDays = selectedDays ^ toggledDay;

        await Context.Interaction.SendResponseAsync(
            InteractionCallback.ModifyMessage(message =>
            {
                message.Content = BuildDaySelectionText(weapon, updatedDays);
                message.Components = ScrimSignupHelper.BuildDaySelectionComponents(signupId, weapon, updatedDays);
            }));
    }

    [ComponentInteraction(ScrimSignupHelper.SaveButtonId)]
    [UsedImplicitly]
    public async Task SaveSignup(long signupId, int weaponValue, int selectedDaysValue)
    {
        if (!ScrimSignupHelper.IsValidWeapon(weaponValue) ||
            !ScrimSignupHelper.IsValidDaySelection(selectedDaysValue))
        {
            await RespondEphemeralAsync("That signup payload is invalid.");
            return;
        }

        var weapon = (ScrimWeapon)weaponValue;
        var selectedDays = (ScrimAvailableDays)selectedDaysValue;

        if (selectedDays == ScrimAvailableDays.None)
        {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.ModifyMessage(message =>
                {
                    message.Content = $"Pick at least one weekday for **{ScrimSignupHelper.FormatWeapon(weapon)}**.\n\n{BuildDaySelectionText(weapon, selectedDays)}";
                    message.Components = ScrimSignupHelper.BuildDaySelectionComponents(signupId, weapon, selectedDays);
                }));
            return;
        }

        // DB write + message refresh can exceed 3 s — keep deferred here
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);

        try
        {
            await scrimService.SaveSignupEntryAsync(signupId, Context.User.Id, weapon, selectedDays);
            await scrimService.RefreshSignupMessageAsync(signupId);

            await Context.Interaction.ModifyFollowupMessageAsync(
                Context.Message.Id,
                message =>
                {
                    message.Content = $"✅ Saved your signup as **{ScrimSignupHelper.FormatWeapon(weapon)}** (**{ScrimSignupHelper.FormatRole(ScrimSignupHelper.GetRoleForWeapon(weapon))}**) for **{ScrimSignupHelper.FormatDays(selectedDays)}**.\n\nUse the select menu below if you want to change your weapon.";
                    message.Components = ScrimSignupHelper.BuildWeaponSelectionComponents(signupId);
                });
        }
        catch (InvalidOperationException ex)
        {
            await Context.Interaction.ModifyFollowupMessageAsync(
                Context.Message.Id,
                message =>
                {
                    message.Content = ex.Message;
                    message.Components = [];
                });
        }
    }

    [ComponentInteraction(ScrimSignupHelper.BackButtonId)]
    [UsedImplicitly]
    public async Task BackToWeaponSelection(long signupId)
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

        var existingEntry = signup.Entries.FirstOrDefault(entry => entry.UserId == Context.User.Id);

        await Context.Interaction.SendResponseAsync(
            InteractionCallback.ModifyMessage(message =>
            {
                message.Content = BuildWeaponSelectionText(existingEntry);
                message.Components = ScrimSignupHelper.BuildWeaponSelectionComponents(signupId);
            }));
    }

    private static string BuildWeaponSelectionText(ScrimSignupEntry? existingEntry)
    {
        if (existingEntry is null)
        {
            return "Choose your weapon from the select menu. The bot derives your role automatically, then you will pick the weekdays you are available.";
        }

        var role = ScrimSignupHelper.ResolveRole(existingEntry);
        return $"Your current signup is **{ScrimSignupHelper.FormatWeapon(existingEntry.Weapon)}** (**{ScrimSignupHelper.FormatRole(role)}**) for **{ScrimSignupHelper.FormatDays(existingEntry.AvailableDays)}**.\nChoose a weapon below to update it.";
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
