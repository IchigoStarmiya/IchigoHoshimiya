using System.Globalization;
using IchigoHoshimiya.Entities.General;
using IchigoHoshimiya.Interfaces;
using NetCord;
using NetCord.Rest;

namespace IchigoHoshimiya.Helpers;

public sealed record ActivityChoiceInfo(ActivityChoice Choice, string Label, string Emoji, ButtonStyle Style);

public class ActivitySignupMessageBuilder(IActivitySignupSchedule schedule) : IActivitySignupMessageBuilder
{
    public const string ChoiceButtonId = "activity-signup";

    public static readonly IReadOnlyList<ActivityChoiceInfo> Activities =
    [
        new(ActivityChoice.ConquestDungeons, "Conquest Dungeons", "🏰", ButtonStyle.Primary),
        new(ActivityChoice.Transcendence, "Transcendence", "✨", ButtonStyle.Primary),
        new(ActivityChoice.Abyss, "Abyss", "🌀", ButtonStyle.Primary),
        new(ActivityChoice.BattlefieldArena, "Battlefield/Arena", "⚔️", ButtonStyle.Primary),
        new(ActivityChoice.BurnAltEnergy, "Burn Alt Energy", "🔋", ButtonStyle.Primary)
    ];

    public static readonly IReadOnlyList<ActivityChoiceInfo> Attendance =
    [
        new(ActivityChoice.Available, "Available", "✅", ButtonStyle.Success),
        new(ActivityChoice.Absent, "Absent", "❌", ButtonStyle.Danger)
    ];

    private const string EmptyField = "-";

    // Field values are capped at 1024 characters by Discord
    private const int MaxFieldLength = 1024;

    public static string BuildChoiceButtonId(long signupId, ActivityChoice choice)
        => $"{ChoiceButtonId}:{signupId}:{(int)choice}";

    public string BuildTitle(ActivitySlot slot)
    {
        var start = schedule.ToLocal(slot.StartUtc);
        var end = schedule.ToLocal(slot.EndUtc);

        return $"{start.ToString("dddd d MMM", CultureInfo.InvariantCulture)} · {FormatHour(start)}-{FormatHour(end)}";
    }

    public EmbedProperties BuildSignupEmbed(ActivitySignup signup)
    {
        var status = signup.IsOpen
            ? $"Signups close <t:{ToUnix(signup.CloseAtUtc)}:R>."
            : "🔒 Signups are closed.";

        var siege = signup.IsSiegeDay
            ? "\n⚠️ **Artifact Siege at 9pm is mandatory** for everyone today."
            : string.Empty;

        var description = $"<t:{ToUnix(signup.SlotStartUtc)}:F> - <t:{ToUnix(signup.SlotEndUtc)}:t>{siege}\n{status}";

        var fields = Activities.Concat(Attendance)
                               .Select(info => BuildField(info, UsersFor(signup, info.Choice)));

        return EmbedHelper.Build(signup.Title, description)
                          .WithFields(fields)
                          .WithFooter(new EmbedFooterProperties { Text = $"{signup.Entries.Count} signed up" });
    }

    public IReadOnlyList<IMessageComponentProperties> BuildComponents(ActivitySignup signup)
    {
        if (!signup.IsOpen)
        {
            return [];
        }

        return
        [
            new ActionRowProperties(Activities.Select(info => BuildButton(signup.Id, info))),
            new ActionRowProperties(Attendance.Select(info => BuildButton(signup.Id, info)))
        ];
    }

    public EmbedProperties BuildTally(ActivitySignup signup)
    {
        var activityLines = Activities.Select(info => (Info: info, Count: UsersFor(signup, info.Choice).Count))
                                      .OrderByDescending(tally => tally.Count)
                                      .Select(tally => $"{tally.Info.Emoji} **{tally.Info.Label}**: {tally.Count}");

        var attendanceLines = Attendance.Select(info =>
            $"{info.Emoji} **{info.Label}**: {UsersFor(signup, info.Choice).Count}");

        var description = string.Join('\n', activityLines) + "\n\n" + string.Join('\n', attendanceLines);

        return EmbedHelper.Build($"📊 Tally · {signup.Title}", description);
    }

    private static EmbedFieldProperties BuildField(ActivityChoiceInfo info, IReadOnlyList<ulong> users)
    {
        var value = users.Count == 0 ? EmptyField : string.Join('\n', users.Select(id => $"<@{id}>"));

        if (value.Length > MaxFieldLength)
        {
            value = value[..value.LastIndexOf('\n', MaxFieldLength - 10)] + "\n...";
        }

        return new EmbedFieldProperties
        {
            Name = $"{info.Emoji} {info.Label} ({users.Count})",
            Value = value,
            Inline = true
        };
    }

    private static ButtonProperties BuildButton(long signupId, ActivityChoiceInfo info)
    {
        return new ButtonProperties(BuildChoiceButtonId(signupId, info.Choice), $"{info.Emoji} {info.Label}", info.Style);
    }

    private static IReadOnlyList<ulong> UsersFor(ActivitySignup signup, ActivityChoice choice)
    {
        return signup.Entries
                     .Where(entry => entry.Choice == choice)
                     .OrderBy(entry => entry.UpdatedAtUtc)
                     .Select(entry => entry.UserId)
                     .ToList();
    }

    private static string FormatHour(DateTime local)
    {
        return local.ToString(local.Minute == 0 ? "htt" : "h:mmtt", CultureInfo.InvariantCulture).ToLowerInvariant();
    }

    private static long ToUnix(DateTime utc)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToUnixTimeSeconds();
    }
}
