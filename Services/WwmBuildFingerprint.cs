using System.Security.Cryptography;
using System.Text;
using IchigoHoshimiya.Entities.Wwm;

namespace IchigoHoshimiya.Services;

// Identifies a build by its major choices only: weapons, inners (xinfa), mystics (skills), the gear
// actually equipped, and which attunements are slotted and active.
//
// Rolled numbers are deliberately excluded — affix and base-attr values drift with every retone,
// durability decays with play, and retone counters move on their own. Those all still get stored on
// each snapshot that does get written; they just do not by themselves count as a new build.
public static class WwmBuildFingerprint
{
    private const string AttunementType = "attunement";

    public static string Compute(WwmPlayerSnapshot snapshot)
    {
        var builder = new StringBuilder();

        builder.Append("weapons|")
               .Append(Scalar(snapshot.MainWeaponId))
               .Append('|')
               .Append(Scalar(snapshot.SubWeaponId))
               .Append('\n');

        builder.Append("inners|");

        foreach (var xinfa in snapshot.Xinfa.OrderBy(x => x.Ordinal))
        {
            builder.Append(xinfa.XinfaId).Append(',');
        }

        builder.Append('\n').Append("mystics|");

        foreach (var skill in snapshot.Skills.OrderBy(s => s.Slot))
        {
            builder.Append(skill.Slot).Append(':').Append(skill.SkillId).Append(',');
        }

        builder.Append('\n');

        foreach (var gear in snapshot.Gear.OrderBy(g => g.SlotOrd))
        {
            builder.Append("gear|")
                   .Append(gear.SlotOrd)
                   .Append('|')
                   .Append(gear.ItemNo)
                   .Append('|')
                   .Append(Scalar(gear.Suit))
                   .Append('\n');

            var attunements = gear.Affixes
                                  .Where(a => string.Equals(
                                             a.Type,
                                             AttunementType,
                                             StringComparison.OrdinalIgnoreCase))
                                  .OrderBy(a => a.AffixId);

            foreach (var attunement in attunements)
            {
                builder.Append("attunement|")
                       .Append(attunement.AffixId)
                       .Append('|')
                       .Append(Scalar(attunement.Active))
                       .Append('\n');
            }
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static string Scalar(int? value) => value?.ToString() ?? "-";

    private static string Scalar(bool? value) => value?.ToString() ?? "-";
}
