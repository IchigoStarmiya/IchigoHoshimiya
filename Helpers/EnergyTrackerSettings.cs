namespace IchigoHoshimiya.Helpers;

public class EnergyTrackerSettings
{
    public ulong AlertChannelId { get; set; }

    public string TimeZone { get; set; } = "Europe/Zurich";

    public int MaxEnergy { get; set; } = 840;

    public int EnergyPerRecharge { get; set; } = 15;

    // Recharges land on multiples of this from local midnight (00:00, 03:00, 06:00 ...)
    public int RechargeIntervalHours { get; set; } = 3;

    public int EnergyPerClaim { get; set; } = 40;

    public int MainDeductionAmount { get; set; } = 80;

    public int EnergyWarningThreshold { get; set; } = 600;

    public int EnergyCriticalThreshold { get; set; } = 720;

    public int NightmareTicketsPerDay { get; set; } = 2;

    public int MaxNightmareTickets { get; set; } = 14;

    public int NightmareTicketResetHour { get; set; } = 9;
}
