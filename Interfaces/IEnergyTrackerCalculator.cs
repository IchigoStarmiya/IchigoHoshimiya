using IchigoHoshimiya.Entities.General;

namespace IchigoHoshimiya.Interfaces;

public enum EnergyTrackerLevel
{
    Normal,
    Warning,
    Critical
}

public interface IEnergyTrackerCalculator
{
    bool ApplyRecharges(EnergyCharacter character, DateTime nowUtc);

    DateTime? GetEnergyFullAtUtc(EnergyCharacter character, DateTime nowUtc);

    DateTime GetNextNightmareResetUtc(DateTime nowUtc);

    DateTime GetNextRechargeUtc(DateTime nowUtc);

    int GetClaims(EnergyCharacter character);

    bool TryDeduct(EnergyCharacter character, int amount);

    EnergyTrackerLevel GetEnergyLevel(EnergyCharacter character);

    EnergyTrackerLevel GetNightmareTicketLevel(EnergyCharacter character);

    void AcknowledgeAlerts(EnergyCharacter character);
}
