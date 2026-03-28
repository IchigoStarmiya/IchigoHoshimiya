using IchigoHoshimiya.Entities.General;
using NetCord;
using NetCord.Rest;

namespace IchigoHoshimiya.Helpers;

public sealed record ScrimWeaponInfo(
    ScrimWeapon Weapon,
    ScrimRole Role,
    string Label,
    string ShortLabel,
    string Description);

public static class ScrimSignupHelper
{
    public const string PublicSignupButtonId = "scrim-open";
    public const string WithdrawButtonId = "scrim-withdraw";
    public const string WeaponMenuId = "scrim-weapon";
    public const string DayToggleButtonId = "scrim-day";
    public const string SaveButtonId = "scrim-save";
    public const string BackButtonId = "scrim-back";
    public const string NotAvailableButtonId = "scrim-notavail";

    public static readonly IReadOnlyList<(ScrimAvailableDays Flag, string Label, string ShortLabel)> Weekdays =
    [
        (ScrimAvailableDays.Monday, "Monday", "Mon"),
        (ScrimAvailableDays.Tuesday, "Tuesday", "Tue"),
        (ScrimAvailableDays.Wednesday, "Wednesday", "Wed"),
        (ScrimAvailableDays.Thursday, "Thursday", "Thu"),
        (ScrimAvailableDays.Friday, "Friday", "Fri")
    ];

    public static readonly IReadOnlyList<ScrimWeaponInfo> WeaponCatalog =
    [
        new(ScrimWeapon.PanaceaFan, ScrimRole.Healer, "Panacea Fan", "Healer", "Healer"),
        new(ScrimWeapon.NamelessSword, ScrimRole.Dps, "Nameless x2", "Namelessx2", "DPS"),
        new(ScrimWeapon.TwinbladeRopeDart, ScrimRole.Dps, "Twinblade/Ropedart", "TB/RD", "DPS"),
        new(ScrimWeapon.MoSb, ScrimRole.Tank, "Moblade/Stormbreaker", "Mo/SB", "Tank"),
        new(ScrimWeapon.MoHybrid, ScrimRole.Tank, "Moblade/Hybrid", "MoHybrid", "Tank"),
        new(ScrimWeapon.Unspecified, ScrimRole.Dps, "Other weapon", "Other", "DPS")
    ];

    public static string BuildPublicSignupButtonId(long signupId) => $"{PublicSignupButtonId}:{signupId}";

    public static string BuildWithdrawButtonId(long signupId) => $"{WithdrawButtonId}:{signupId}";

    public static string BuildWeaponMenuId(long signupId) => $"{WeaponMenuId}:{signupId}";

    public static string BuildDayToggleButtonId(long signupId, ScrimWeapon weapon, ScrimAvailableDays selectedDays, ScrimAvailableDays day)
        => $"{DayToggleButtonId}:{signupId}:{(int)weapon}:{(int)selectedDays}:{(int)day}";

    public static string BuildSaveButtonId(long signupId, ScrimWeapon weapon, ScrimAvailableDays selectedDays)
        => $"{SaveButtonId}:{signupId}:{(int)weapon}:{(int)selectedDays}";

    public static string BuildNotAvailableButtonId(long signupId, ScrimWeapon weapon)
        => $"{NotAvailableButtonId}:{signupId}:{(int)weapon}";

    public static string BuildBackButtonId(long signupId) => $"{BackButtonId}:{signupId}";

    public static IReadOnlyList<ActionRowProperties> BuildPublicComponents(long signupId)
    {
        return
        [
            new ActionRowProperties(
            [
                new ButtonProperties(BuildPublicSignupButtonId(signupId), "Sign Up / Update", ButtonStyle.Primary),
                new ButtonProperties(BuildWithdrawButtonId(signupId), "Withdraw", ButtonStyle.Danger)
            ])
        ];
    }

    public static IReadOnlyList<IMessageComponentProperties> BuildWeaponSelectionComponents(long signupId, ScrimWeapon? selectedWeapon = null)
    {
        var options = WeaponCatalog
            .Select(weapon => new StringMenuSelectOptionProperties(
                weapon.Label,
                ((int)weapon.Weapon).ToString())
            {
                Description = weapon.Description,
                Default = selectedWeapon == weapon.Weapon
            })
            .ToList();

        IMessageComponentProperties weaponMenu = new StringMenuProperties(BuildWeaponMenuId(signupId), options)
        {
            Placeholder = "Choose your weapon",
            MinValues = 1,
            MaxValues = 1
        };

        return [weaponMenu];
    }

    public static IReadOnlyList<ActionRowProperties> BuildDaySelectionComponents(long signupId, ScrimWeapon weapon, ScrimAvailableDays selectedDays)
    {
        var dayButtons = Weekdays
            .Select(day => new ButtonProperties(
                BuildDayToggleButtonId(signupId, weapon, selectedDays, day.Flag),
                day.ShortLabel,
                selectedDays.HasFlag(day.Flag) ? ButtonStyle.Success : ButtonStyle.Secondary))
            .ToList();

        return
        [
            new ActionRowProperties(dayButtons),
            new ActionRowProperties(
            [
                new ButtonProperties(BuildSaveButtonId(signupId, weapon, selectedDays), "Save Signup", ButtonStyle.Primary),
                new ButtonProperties(BuildNotAvailableButtonId(signupId, weapon), "Not Available This Week", ButtonStyle.Danger),
                new ButtonProperties(BuildBackButtonId(signupId), "Back", ButtonStyle.Secondary)
            ])
        ];
    }

    public static ScrimRole GetRoleForWeapon(ScrimWeapon weapon)
    {
        return WeaponCatalog.FirstOrDefault(info => info.Weapon == weapon)?.Role ?? ScrimRole.Dps;
    }

    public static string FormatWeapon(ScrimWeapon weapon)
    {
        return WeaponCatalog.FirstOrDefault(info => info.Weapon == weapon)?.Label ?? "Unspecified";
    }

    public static string FormatWeaponShort(ScrimWeapon weapon)
    {
        return WeaponCatalog.FirstOrDefault(info => info.Weapon == weapon)?.ShortLabel ?? "UNK";
    }

    public static ScrimRole ResolveRole(ScrimSignupEntry entry)
    {
        return GetRoleForWeapon(entry.Weapon);
    }

    public static string FormatRole(ScrimRole role)
    {
        return role switch
        {
            ScrimRole.Healer => "Healer",
            ScrimRole.Tank => "Tank",
            ScrimRole.Dps => "DPS",
            _ => "Unknown"
        };
    }

    public static string FormatDays(ScrimAvailableDays days)
    {
        var labels = Weekdays
            .Where(day => days.HasFlag(day.Flag))
            .Select(day => day.Label)
            .ToList();

        return labels.Count == 0 ? "Not available this week" : string.Join(", ", labels);
    }

    public static string FormatDaysShort(ScrimAvailableDays days)
    {
        var labels = Weekdays
            .Where(day => days.HasFlag(day.Flag))
            .Select(day => day.ShortLabel)
            .ToList();

        return labels.Count == 0 ? "N/A" : string.Join('/', labels);
    }

    public static string FormatSingleDayShort(ScrimAvailableDays day)
    {
        return Weekdays.FirstOrDefault(weekday => weekday.Flag == day).ShortLabel ?? day.ToString();
    }

    public static string FormatSingleDay(ScrimAvailableDays day)
    {
        return Weekdays.FirstOrDefault(weekday => weekday.Flag == day).Label ?? day.ToString();
    }

    public static string FormatRoleShort(ScrimRole role)
    {
        return role switch
        {
            ScrimRole.Healer => "H",
            ScrimRole.Tank => "T",
            ScrimRole.Dps => "D",
            _ => "?"
        };
    }

    public static bool IsValidWeapon(int value)
    {
        return Enum.IsDefined(typeof(ScrimWeapon), value);
    }

    public static bool IsValidDaySelection(int value)
    {
        const int allowedMask = (int)(ScrimAvailableDays.Monday |
                                      ScrimAvailableDays.Tuesday |
                                      ScrimAvailableDays.Wednesday |
                                      ScrimAvailableDays.Thursday |
                                      ScrimAvailableDays.Friday);

        return value >= 0 && (value & ~allowedMask) == 0;
    }
}

