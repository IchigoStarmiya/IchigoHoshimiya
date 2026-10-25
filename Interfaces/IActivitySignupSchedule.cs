namespace IchigoHoshimiya.Interfaces;

public sealed record ActivitySlot(
    DateTime StartUtc,
    DateTime EndUtc,
    DateTime PostAtUtc,
    DateTime CloseAtUtc,
    bool IsSiegeDay);

public interface IActivitySignupSchedule
{
    // Slots whose signup should currently be up, in the order they should be posted
    IReadOnlyList<ActivitySlot> GetOpenSlots(DateTime nowUtc);

    DateTime GetNextPostUtc(DateTime nowUtc);

    DateTime ToLocal(DateTime utc);
}
