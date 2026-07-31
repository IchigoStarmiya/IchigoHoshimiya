using System.Globalization;
using System.Text;
using IchigoHoshimiya.Entities.Wwm;

namespace IchigoHoshimiya.Helpers;

// Renders full build snapshots as a markdown document. Unlike WwmProfileFormatter, which trims to
// fit a Discord embed, this is written to a file and so keeps every rolled value.
public static class WwmBuildMarkdownWriter
{
    private const string AttunementType = "attunement";

    public static string Write(WwmPlayer player, IReadOnlyList<WwmPlayerSnapshot> snapshots, int windowDays)
    {
        var builder = new StringBuilder();

        builder.Append("# ").Append(player.Name).Append(" · ").Append(player.NumberId).Append("\n\n");

        builder.Append("- **Region** ").Append(player.Region ?? "unknown")
               .Append(" · **Server** ").Append(player.Server).Append('\n');

        builder.Append("- **Window** last ").Append(windowDays).Append(" days\n");

        builder.Append("- **Builds recorded** ").Append(snapshots.Count).Append('\n');

        builder.Append("- **Exported** ").Append(Stamp(DateTime.UtcNow)).Append("\n\n");

        builder.Append("> A build is recorded only when the major setup changes — weapons, inner ways,\n")
               .Append("> mystics, equipped gear or attunements. Stat re-rolls alone do not create an entry.\n\n");

        foreach (var snapshot in snapshots)
        {
            WriteSnapshot(builder, snapshot);
        }

        return builder.ToString();
    }

    private static void WriteSnapshot(StringBuilder builder, WwmPlayerSnapshot snapshot)
    {
        builder.Append("---\n\n");

        builder.Append("## ").Append(Stamp(snapshot.FetchedAtUtc)).Append("\n\n");

        builder.Append("- **Level** ").Append(snapshot.Level);

        if (snapshot.School is { } school)
        {
            builder.Append(" · **School** ").Append(school);
        }

        if (snapshot.BuildPower is { } power)
        {
            builder.Append(" · **Build Power** ").Append(power);
        }

        if (snapshot.MaxXiuweiKungfu is { } kungfu)
        {
            builder.Append(" · **Max Kungfu** ").Append(kungfu);
        }

        builder.Append('\n');

        builder.Append("- **Weapons** ")
               .Append(Named(snapshot.MainWeaponName, snapshot.MainWeaponId))
               .Append(" / ")
               .Append(Named(snapshot.SubWeaponName, snapshot.SubWeaponId))
               .Append('\n');

        builder.Append("- **Inner Ways** ")
               .Append(Join(snapshot.Xinfa
                                    .OrderBy(x => x.Ordinal)
                                    .Select(x => Named(x.Name, x.XinfaId))))
               .Append('\n');

        builder.Append("- **Mystics** ")
               .Append(Join(snapshot.Skills
                                    .OrderBy(s => s.Slot)
                                    .Select(s => $"{s.Slot}. {Named(s.Name, s.SkillId)}")))
               .Append('\n');

        if (snapshot.Wins is { } wins && snapshot.Losses is { } losses)
        {
            builder.Append("- **PvP** ").Append(wins).Append("W / ").Append(losses).Append('L');

            if (snapshot.WinRate is { } rate)
            {
                builder.Append(" (").Append((rate * 100).ToString("0.0", CultureInfo.InvariantCulture)).Append("%)");
            }

            builder.Append('\n');
        }

        if (snapshot.GuildName is { Length: > 0 } guild)
        {
            builder.Append("- **Guild** ").Append(Escape(guild)).Append('\n');
        }

        builder.Append("- **Build hash** `").Append(snapshot.BuildHash).Append("`\n\n");

        builder.Append("### Gear\n\n");

        foreach (var gear in snapshot.Gear.OrderBy(g => g.SlotOrd))
        {
            WriteGear(builder, gear);
        }
    }

    private static void WriteGear(StringBuilder builder, WwmGear gear)
    {
        builder.Append("#### ").Append(gear.SlotName ?? $"Slot {gear.SlotOrd}");

        if (gear.SuitName is { Length: > 0 } suit)
        {
            builder.Append(" — ").Append(Escape(suit));
        }

        builder.Append("\n\n");

        builder.Append("Item `").Append(gear.ItemNo).Append('`');

        if (gear.Durability is { } durability)
        {
            builder.Append(" · durability ").Append(durability);
        }

        if (gear.Retoned)
        {
            builder.Append(" · retoned");

            if (gear.RetoneCount is { } count)
            {
                builder.Append(" ×").Append(count);
            }

            if (gear.RetoneAtUtc is { } retonedAt)
            {
                builder.Append(" (").Append(Stamp(retonedAt)).Append(')');
            }
        }

        builder.Append("\n\n");

        var baseAttrs = gear.BaseAttrs.OrderBy(a => a.Ordinal).ToList();

        if (baseAttrs.Count > 0)
        {
            builder.Append("| Base stat | Value |\n|---|---|\n");

            foreach (var attr in baseAttrs)
            {
                builder.Append("| ").Append(Escape(attr.Name ?? attr.RawName ?? "—"))
                       .Append(" | ").Append(Number(attr.Value)).Append(" |\n");
            }

            builder.Append('\n');
        }

        var affixes = gear.Affixes
                          .Where(a => !IsAttunement(a))
                          .OrderBy(a => a.Ordinal)
                          .ToList();

        if (affixes.Count > 0)
        {
            builder.Append("| Affix | Value |\n|---|---|\n");

            foreach (var affix in affixes)
            {
                builder.Append("| ").Append(AffixName(affix))
                       .Append(" | ").Append(Display(affix)).Append(" |\n");
            }

            builder.Append('\n');
        }

        var attunements = gear.Affixes
                              .Where(IsAttunement)
                              .OrderBy(a => a.Ordinal)
                              .ToList();

        if (attunements.Count > 0)
        {
            builder.Append("**Attunements**\n\n");

            foreach (var attunement in attunements)
            {
                builder.Append(attunement.Active == true ? "- ✅ " : "- ⬜ ")
                       .Append(AffixName(attunement));

                // Effect-style attunements carry a placeholder value of 1 with no display string;
                // printing it would read as a stat.
                if (attunement.DisplayValue is { Length: > 0 } display)
                {
                    builder.Append(" — ").Append(display);
                }

                builder.Append('\n');
            }

            builder.Append('\n');
        }
    }

    private static bool IsAttunement(WwmGearAffix affix) =>
        string.Equals(affix.Type, AttunementType, StringComparison.OrdinalIgnoreCase);

    // Some affixes come back with an empty name, so fall back to the id rather than a blank cell.
    private static string AffixName(WwmGearAffix affix) =>
        string.IsNullOrWhiteSpace(affix.Name) ? $"affix #{affix.AffixId}" : Escape(affix.Name);

    private static string Display(WwmGearAffix affix) =>
        string.IsNullOrWhiteSpace(affix.DisplayValue) ? Number(affix.Value) : affix.DisplayValue;

    private static string Number(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);

    private static string Named(string? name, int? id) =>
        name is { Length: > 0 }
            ? $"{Escape(name)} ({id?.ToString(CultureInfo.InvariantCulture) ?? "?"})"
            : id?.ToString(CultureInfo.InvariantCulture) ?? "—";

    private static string Join(IEnumerable<string> values)
    {
        var joined = string.Join(", ", values);

        return joined.Length == 0 ? "—" : joined;
    }

    private static string Stamp(DateTime value) =>
        value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " UTC";

    // Pipes would break table rows and newlines would break the row entirely; attunement text is
    // free-form and contains both.
    private static string Escape(string value) =>
        value.Replace("|", "\\|").Replace('\n', ' ').Replace('\r', ' ').Trim();
}
