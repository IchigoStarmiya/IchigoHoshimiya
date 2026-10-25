using IchigoHoshimiya.Helpers;
using IchigoHoshimiya.Interfaces;
using Microsoft.Extensions.Options;

namespace IchigoHoshimiya.Services;

public class ActivitySignupSchedule : IActivitySignupSchedule
{
    // All hours are relative to local midnight of the slot's day, so -3 is 21:00 the day before
    private sealed record SlotTemplate(int StartHour, int EndHour, int PostHour, int CloseHour);

    private const int WeekdayPostHour = 9;
    private const int WeekdayCloseHour = 16;

    private static readonly HashSet<DayOfWeek> SiegeDays = [DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Saturday];

    private readonly TimeZoneInfo _timeZone;

    public ActivitySignupSchedule(IOptions<ActivitySignupSettings> options)
    {
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);
    }

    public IReadOnlyList<ActivitySlot> GetOpenSlots(DateTime nowUtc)
    {
        return SlotsAround(nowUtc)
              .Where(slot => slot.PostAtUtc <= nowUtc && nowUtc < slot.CloseAtUtc)
              .ToList();
    }

    public DateTime GetNextPostUtc(DateTime nowUtc)
    {
        return SlotsAround(nowUtc).First(slot => slot.PostAtUtc > nowUtc).PostAtUtc;
    }

    public DateTime ToLocal(DateTime utc)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utc, _timeZone);
    }

    // Posts go up at most a day ahead of their slot and never more than a day apart,
    // so a few days either side always covers every open slot and the next post
    private IEnumerable<ActivitySlot> SlotsAround(DateTime nowUtc)
    {
        var today = DateOnly.FromDateTime(ToLocal(nowUtc));

        return Enumerable.Range(-1, 4)
                         .SelectMany(offset => SlotsFor(today.AddDays(offset)))
                         .OrderBy(slot => slot.PostAtUtc)
                         .ThenBy(slot => slot.StartUtc);
    }

    private IEnumerable<ActivitySlot> SlotsFor(DateOnly date)
    {
        var isSiegeDay = SiegeDays.Contains(date.DayOfWeek);

        return Templates(date.DayOfWeek, isSiegeDay)
           .Select(template => new ActivitySlot(
                ToUtc(date, template.StartHour),
                ToUtc(date, template.EndHour),
                ToUtc(date, template.PostHour),
                ToUtc(date, template.CloseHour),
                isSiegeDay));
    }

    private static IEnumerable<SlotTemplate> Templates(DayOfWeek day, bool isSiegeDay)
    {
        // Siege starts at 21:00, so the evening is trimmed around it
        (int Start, int End) early = isSiegeDay ? (18, 19) : (18, 21);
        (int Start, int End) late = isSiegeDay ? (22, 24) : (21, 24);

        if (day is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
        {
            return
            [
                new SlotTemplate(early.Start, early.End, WeekdayPostHour, WeekdayCloseHour),
                new SlotTemplate(late.Start, late.End, WeekdayPostHour, WeekdayCloseHour)
            ];
        }

        return new (int Start, int End, int Post)[]
               {
                   (9, 12, -3),
                   (12, 15, 9),
                   (15, 18, 12),
                   (early.Start, early.End, 15),
                   (late.Start, late.End, 21)
               }
              .Select(slot => new SlotTemplate(
                   slot.Start,
                   slot.End,
                   slot.Post,
                   // A slot posted as it starts stays open until it ends instead of closing immediately
                   slot.Post < slot.Start ? slot.Start : slot.End));
    }

    private DateTime ToUtc(DateOnly date, int hour)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified).AddHours(hour);

        return TimeZoneInfo.ConvertTimeToUtc(local, _timeZone);
    }
}
