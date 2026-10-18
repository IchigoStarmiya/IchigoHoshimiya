using System.Text;
using IchigoHoshimiya.Entities.General;
using IchigoHoshimiya.Interfaces;
using Microsoft.Extensions.Options;
using NetCord.Rest;

namespace IchigoHoshimiya.Helpers;

public class EnergyTrackerEmbedBuilder(IEnergyTrackerCalculator calculator, IOptions<EnergyTrackerSettings> options)
    : IEnergyTrackerEmbedBuilder
{
    // Discord caps descriptions at 4096 characters and all embeds in one message at 6000,
    // so every page is meant to be sent as its own message
    private const int MaxDescriptionLength = 4000;

    private readonly EnergyTrackerSettings _settings = options.Value;

    public IReadOnlyList<EmbedProperties> BuildList(string ownerName, IReadOnlyList<EnergyCharacter> characters)
    {
        var title = $"{ownerName}'s characters";

        if (characters.Count == 0)
        {
            return [EmbedHelper.Build(title, "No characters yet. Add one with `/energy add`.")];
        }

        var now = DateTime.UtcNow;

        return Paginate(title, characters.Select(c => Describe(c, now) + "\n"));
    }

    public IReadOnlyList<EmbedProperties> BuildOverview(IReadOnlyList<EnergyCharacter> characters)
    {
        const string title = "Energy overview";

        if (characters.Count == 0)
        {
            return [EmbedHelper.Build(title, "Nobody is tracking any characters yet.")];
        }

        // One entry per line so a user with many characters can still be split across pages
        var entries = characters.GroupBy(c => c.UserId)
                                .SelectMany(group => group.Select(DescribeCompact).Prepend($"\n<@{group.Key}>"));

        return Paginate(title, entries);
    }

    public IReadOnlyList<EmbedProperties> BuildDeductionSummary(
        int amount,
        IReadOnlyList<EnergyDeductionResult> results)
    {
        var title = $"Deducted {amount} energy from main characters";

        if (results.Count == 0)
        {
            return [EmbedHelper.Build(title, "Nobody has a main character. Mark one with `/energy set main:True`.")];
        }

        var entries = results.Select(r =>
        {
            var owner = $"<@{r.Character.UserId}> **{r.Character.Name}**";

            return r.Deducted
                ? $"✅ {owner}: energy {r.EnergyBefore} → {r.Character.Energy}, " +
                  $"extra {r.ExtraEnergyBefore} → {r.Character.ExtraEnergy}"
                : $"❌ {owner}: only {r.EnergyBefore + r.ExtraEnergyBefore} energy in total, skipped";
        });

        return Paginate(title, entries);
    }

    private static IReadOnlyList<EmbedProperties> Paginate(string title, IEnumerable<string> entries)
    {
        var pages = new List<StringBuilder> { new() };

        foreach (var entry in entries)
        {
            if (pages[^1].Length > 0 && pages[^1].Length + entry.Length > MaxDescriptionLength)
            {
                pages.Add(new StringBuilder());
            }

            pages[^1].Append(entry).Append('\n');
        }

        return pages.Select((page, i) => EmbedHelper.Build(i == 0 ? title : null, page.ToString().TrimEnd()))
                    .ToList();
    }

    private string Describe(EnergyCharacter character, DateTime now)
    {
        var itemLevel = character.ItemLevel is { } level ? $" · iL {level}" : string.Empty;

        var fullIn = calculator.GetEnergyFullAtUtc(character, now) is { } fullAt
            ? $"full <t:{new DateTimeOffset(fullAt).ToUnixTimeSeconds()}:R>"
            : "**Full**";

        return $"{Indicator(calculator.GetEnergyLevel(character))} {MainMarker(character)}**{character.Name}**{itemLevel}\n" +
               $"Energy {character.Energy}/{_settings.MaxEnergy} ({fullIn}) · " +
               $"Extra {character.ExtraEnergy} · Claims {calculator.GetClaims(character)}\n" +
               $"{Indicator(calculator.GetNightmareTicketLevel(character))} Nightmare tickets " +
               $"{character.NightmareTickets}/{_settings.MaxNightmareTickets}";
    }

    private string DescribeCompact(EnergyCharacter character)
    {
        return $"{Indicator(calculator.GetEnergyLevel(character))} {MainMarker(character)}**{character.Name}** " +
               $"{character.Energy}/{_settings.MaxEnergy} +{character.ExtraEnergy} · " +
               $"{calculator.GetClaims(character)} claims · " +
               $"{Indicator(calculator.GetNightmareTicketLevel(character))} " +
               $"{character.NightmareTickets}/{_settings.MaxNightmareTickets} tickets";
    }

    private static string MainMarker(EnergyCharacter character)
    {
        return character.IsMain ? "⭐ " : string.Empty;
    }

    private static string Indicator(EnergyTrackerLevel level)
    {
        return level switch
        {
            EnergyTrackerLevel.Critical => "🔴",
            EnergyTrackerLevel.Warning => "🟡",
            _ => "🟢"
        };
    }
}
