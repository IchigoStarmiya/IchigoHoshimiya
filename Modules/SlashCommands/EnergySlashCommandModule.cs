using IchigoHoshimiya.Helpers;
using IchigoHoshimiya.Interfaces;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace IchigoHoshimiya.Modules.SlashCommands;

[SlashCommand("energy", "Track your characters' energy and nightmare tickets")]
public class EnergySlashCommandModule(
    IEnergyTrackerService energyTrackerService,
    IEnergyTrackerEmbedBuilder embedBuilder,
    IOptions<EnergyTrackerSettings> options,
    IConfiguration configuration)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("add", "Add a character to track")]
    [UsedImplicitly]
    public async Task AddCharacter(
        [SlashCommandParameter(Name = "name", Description = "Character name", MaxLength = 100)]
        string name,
        [SlashCommandParameter(Name = "energy", Description = "Current energy", MinValue = 0)]
        int energy = 0,
        [SlashCommandParameter(Name = "nightmare_tickets", Description = "Nightmare tickets left", MinValue = 0)]
        int nightmareTickets = 0,
        [SlashCommandParameter(Name = "extra_energy", Description = "Energy outside the regular pool", MinValue = 0)]
        int extraEnergy = 0,
        [SlashCommandParameter(Name = "item_level", Description = "Item level", MinValue = 0)]
        int? itemLevel = null,
        [SlashCommandParameter(Name = "main", Description = "Whether this is a main character")]
        bool main = false)
    {
        var result = await energyTrackerService.AddCharacterAsync(
            Context.User.Id,
            name,
            new EnergyCharacterUpdate(energy, extraEnergy, nightmareTickets, itemLevel, main));

        await RespondAsync(result switch
        {
            EnergyCharacterResult.NameTaken => Ephemeral($"You already have a character called **{name}**."),
            _ => Ephemeral($"✅ Now tracking **{name}**.")
        });
    }

    [SubSlashCommand("set", "Overwrite a character's values, e.g. after spending energy")]
    [UsedImplicitly]
    public async Task SetCharacter(
        [SlashCommandParameter(Name = "name", Description = "Character name")]
        string name,
        [SlashCommandParameter(Name = "energy", Description = "Current energy", MinValue = 0)]
        int? energy = null,
        [SlashCommandParameter(Name = "nightmare_tickets", Description = "Nightmare tickets left", MinValue = 0)]
        int? nightmareTickets = null,
        [SlashCommandParameter(Name = "extra_energy", Description = "Energy outside the regular pool", MinValue = 0)]
        int? extraEnergy = null,
        [SlashCommandParameter(Name = "item_level", Description = "Item level", MinValue = 0)]
        int? itemLevel = null,
        [SlashCommandParameter(Name = "main", Description = "Whether this is a main character")]
        bool? main = null,
        [SlashCommandParameter(Name = "rename", Description = "New character name", MaxLength = 100)]
        string? rename = null)
    {
        var result = await energyTrackerService.UpdateCharacterAsync(
            Context.User.Id,
            name,
            new EnergyCharacterUpdate(energy, extraEnergy, nightmareTickets, itemLevel, main, rename));

        await RespondAsync(result switch
        {
            EnergyCharacterResult.NotFound => Ephemeral($"You don't have a character called **{name}**."),
            EnergyCharacterResult.NameTaken => Ephemeral($"You already have a character called **{rename}**."),
            _ => Ephemeral($"✅ Updated **{rename ?? name}**.")
        });
    }

    [SubSlashCommand("remove", "Stop tracking a character")]
    [UsedImplicitly]
    public async Task RemoveCharacter(
        [SlashCommandParameter(Name = "name", Description = "Character name")]
        string name)
    {
        var result = await energyTrackerService.RemoveCharacterAsync(Context.User.Id, name);

        await RespondAsync(result switch
        {
            EnergyCharacterResult.NotFound => Ephemeral($"You don't have a character called **{name}**."),
            _ => Ephemeral($"🗑️ Stopped tracking **{name}**.")
        });
    }

    [SubSlashCommand("list", "Show tracked characters")]
    [UsedImplicitly]
    public async Task ListCharacters(
        [SlashCommandParameter(Name = "user", Description = "Whose characters to show (defaults to you)")]
        User? user = null)
    {
        await RespondAsync(InteractionCallback.DeferredMessage());

        var owner = user ?? Context.User;
        var characters = await energyTrackerService.GetCharactersAsync(owner.Id);

        await SendPagesAsync(embedBuilder.BuildList(owner.Username, characters));
    }

    [SubSlashCommand("overview", "Show everyone's characters")]
    [UsedImplicitly]
    public async Task Overview()
    {
        await RespondAsync(InteractionCallback.DeferredMessage());

        var characters = await energyTrackerService.GetAllCharactersAsync();

        await SendPagesAsync(embedBuilder.BuildOverview(characters));
    }

    [SubSlashCommand("deduct", "Deduct energy from every main character (bot owner only)")]
    [UsedImplicitly]
    public async Task DeductFromMainCharacters(
        [SlashCommandParameter(Name = "amount", Description = "Energy to deduct (defaults to 80)", MinValue = 1)]
        int? amount = null)
    {
        var ownerUserId = configuration.GetValue<ulong>("Discord:OwnerUserId");

        if (ownerUserId == 0 || Context.User.Id != ownerUserId)
        {
            await RespondAsync(Ephemeral("Only the bot owner can use this command."));
            return;
        }

        await RespondAsync(InteractionCallback.DeferredMessage());

        var deduction = amount ?? options.Value.MainDeductionAmount;
        var results = await energyTrackerService.DeductFromMainCharactersAsync(deduction);

        await SendPagesAsync(embedBuilder.BuildDeductionSummary(deduction, results));
    }

    private async Task SendPagesAsync(IReadOnlyList<EmbedProperties> pages)
    {
        await Context.Interaction.ModifyResponseAsync(message => message.Embeds = [pages[0]]);

        foreach (var page in pages.Skip(1))
        {
            await Context.Interaction.SendFollowupMessageAsync(new InteractionMessageProperties
            {
                Embeds = [page]
            });
        }
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
