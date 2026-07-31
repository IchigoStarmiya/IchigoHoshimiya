using System.Globalization;
using System.Text;
using IchigoHoshimiya.Entities.Wwm;

namespace IchigoHoshimiya.Helpers;

// Renders build snapshots into Discord-safe text. Discord caps an embed description at 4096
// characters, so callers ask for a number of snapshots and get back however many actually fit.
public static class WwmProfileFormatter
{
    // Discord's hard limit is 4096; the margin absorbs the trailing separator on the last block.
    private const int DescriptionBudget = 4050;

    private const int AttunementNameLimit = 58;

    public static string BuildOverview(IReadOnlyList<WwmPlayerSnapshot> snapshots, out int rendered)
    {
        var builder = new StringBuilder();

        rendered = 0;

        for (var i = 0; i < snapshots.Count; i++)
        {
            var block = RenderSnapshot(snapshots[i], i == 0);

            if (builder.Length + block.Length > DescriptionBudget)
            {
                break;
            }

            builder.Append(block);
            rendered++;
        }

        if (rendered == 0 && snapshots.Count > 0)
        {
            // A single build too large to fit is still worth showing in part.
            builder.Append(Truncate(RenderSnapshot(snapshots[0], true), DescriptionBudget));
            rendered = 1;
        }

        return builder.ToString();
    }

    private static string RenderSnapshot(WwmPlayerSnapshot snapshot, bool isCurrent)
    {
        var builder = new StringBuilder();

        var stamp = new DateTimeOffset(snapshot.FetchedAtUtc, TimeSpan.Zero).ToUnixTimeSeconds();

        builder.Append("### <t:").Append(stamp).Append(":D>");

        if (isCurrent)
        {
            builder.Append(" · current");
        }

        builder.Append('\n');

        builder.Append("**Weapons** ")
               .Append(Weapon(snapshot.MainWeaponName, snapshot.MainWeaponId))
               .Append(" / ")
               .Append(Weapon(snapshot.SubWeaponName, snapshot.SubWeaponId))
               .Append('\n');

        var inners = snapshot.Xinfa
                             .OrderBy(x => x.Ordinal)
                             .Select(x => x.Name ?? x.XinfaId.ToString(CultureInfo.InvariantCulture));

        builder.Append("**Inner Ways** ").Append(Join(inners)).Append('\n');

        var mystics = snapshot.Skills
                              .OrderBy(s => s.Slot)
                              .Select(s => s.Name ?? s.SkillId.ToString(CultureInfo.InvariantCulture));

        builder.Append("**Mystics** ").Append(Join(mystics)).Append('\n');

        var suits = snapshot.Gear
                            .Where(g => !string.IsNullOrEmpty(g.SuitName))
                            .GroupBy(g => g.SuitName!)
                            .OrderByDescending(g => g.Count())
                            .Select(g => $"{g.Key} ×{g.Count()}");

        builder.Append("**Gear** ").Append(Join(suits)).Append('\n');

        var attunements = snapshot.Gear
                                  .OrderBy(g => g.SlotOrd)
                                  .SelectMany(
                                       g => g.Affixes
                                             .Where(a => a.Active == true &&
                                                         string.Equals(
                                                             a.Type,
                                                             "attunement",
                                                             StringComparison.OrdinalIgnoreCase))
                                             .Select(a => $"`{g.SlotName}` {Truncate(Clean(a.Name, a.AffixId), AttunementNameLimit)}"))
                                  .ToList();

        builder.Append("**Attunements**\n");

        if (attunements.Count == 0)
        {
            builder.Append("_none active_\n");
        }
        else
        {
            foreach (var attunement in attunements)
            {
                builder.Append("• ").Append(attunement).Append('\n');
            }
        }

        builder.Append('\n');

        return builder.ToString();
    }

    private static string Weapon(string? name, int? id) =>
        name ?? id?.ToString(CultureInfo.InvariantCulture) ?? "—";

    private static string Join(IEnumerable<string> values)
    {
        var joined = string.Join(", ", values);

        return string.IsNullOrEmpty(joined) ? "—" : joined;
    }

    // Attunement text is free-form and can contain newlines and markdown-hostile characters.
    // Some entries carry no name at all, so fall back to the id rather than an anonymous label.
    private static string Clean(string? value, long affixId) =>
        string.IsNullOrWhiteSpace(value)
            ? $"attunement #{affixId}"
            : value.Replace('\n', ' ').Replace('\r', ' ').Replace("`", "'");

    private static string Truncate(string value, int maxLength) =>
        value.Length > maxLength ? value[..(maxLength - 1)] + "…" : value;
}
