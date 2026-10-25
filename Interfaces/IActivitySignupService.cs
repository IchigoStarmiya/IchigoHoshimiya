using IchigoHoshimiya.Entities.General;

namespace IchigoHoshimiya.Interfaces;

public enum ActivityChoiceOutcome
{
    NotFound,
    Closed,
    Chosen,
    Withdrawn
}

public record ActivityChoiceResult(ActivityChoiceOutcome Outcome, ActivitySignup? Signup = null);

public interface IActivitySignupService
{
    // Posts every scheduled signup that should be up but isn't yet
    Task PostDueSignupsAsync(ulong channelId, DateTime nowUtc);

    Task<ActivitySignup> PostTestSignupAsync(ulong channelId, TimeSpan closesIn);

    // Picking the choice you already have withdraws you instead
    Task<ActivityChoiceResult> ToggleChoiceAsync(long signupId, ulong userId, ActivityChoice choice);

    Task CloseDueSignupsAsync(DateTime nowUtc);

    Task<DateTime?> GetNextCloseUtcAsync();
}
