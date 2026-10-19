using IchigoHoshimiya.Entities.General;
using IchigoHoshimiya.Helpers;
using IchigoHoshimiya.Interfaces;
using Microsoft.Extensions.Options;

namespace IchigoHoshimiya.Services;

public class EnergyTrackerCalculator : IEnergyTrackerCalculator
{
    private readonly EnergyTrackerSettings _settings;
    private readonly TimeZoneInfo _timeZone;
    private readonly TimeSpan[] _energyMarks;
    private readonly TimeSpan[] _ticketMarks;

    public EnergyTrackerCalculator(IOptions<EnergyTrackerSettings> options)
    {
        _settings = options.Value;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(_settings.TimeZone);

        _energyMarks = Enumerable.Range(0, 24 / _settings.RechargeIntervalHours)
                                 .Select(i => TimeSpan.FromHours(i * _settings.RechargeIntervalHours))
                                 .ToArray();

        _ticketMarks = [TimeSpan.FromHours(_settings.NightmareTicketResetHour)];
    }

    public bool ApplyRecharges(EnergyCharacter character, DateTime nowUtc)
    {
        var energyRecharges = CountMarksBetween(_energyMarks, character.EnergyUpdatedAtUtc, nowUtc);
        var ticketResets = CountMarksBetween(_ticketMarks, character.TicketsUpdatedAtUtc, nowUtc);

        if (energyRecharges > 0)
        {
            if (character.Energy < _settings.MaxEnergy)
            {
                character.Energy = Math.Min(
                    _settings.MaxEnergy,
                    character.Energy + energyRecharges * _settings.EnergyPerRecharge);
            }

            character.EnergyUpdatedAtUtc = nowUtc;
        }

        if (ticketResets > 0)
        {
            if (character.NightmareTickets < _settings.MaxNightmareTickets)
            {
                character.NightmareTickets = Math.Min(
                    _settings.MaxNightmareTickets,
                    character.NightmareTickets + ticketResets * _settings.NightmareTicketsPerDay);
            }

            character.TicketsUpdatedAtUtc = nowUtc;
        }

        return energyRecharges > 0 || ticketResets > 0;
    }

    public DateTime? GetEnergyFullAtUtc(EnergyCharacter character, DateTime nowUtc)
    {
        if (character.Energy >= _settings.MaxEnergy)
        {
            return null;
        }

        var rechargesNeeded = (int)Math.Ceiling(
            (_settings.MaxEnergy - character.Energy) / (double)_settings.EnergyPerRecharge);

        return MarksAfter(_energyMarks, nowUtc).ElementAt(rechargesNeeded - 1);
    }

    public DateTime GetNextNightmareResetUtc(DateTime nowUtc)
    {
        return MarksAfter(_ticketMarks, nowUtc).First();
    }

    public DateTime GetNextRechargeUtc(DateTime nowUtc)
    {
        var nextEnergy = MarksAfter(_energyMarks, nowUtc).First();
        var nextTicket = GetNextNightmareResetUtc(nowUtc);

        return nextEnergy < nextTicket ? nextEnergy : nextTicket;
    }

    public int GetClaims(EnergyCharacter character)
    {
        return (character.Energy + character.ExtraEnergy) / _settings.EnergyPerClaim;
    }

    public bool TryDeduct(EnergyCharacter character, int amount)
    {
        if (character.Energy + character.ExtraEnergy < amount)
        {
            return false;
        }

        // Regular energy is spent before extra energy
        var fromEnergy = Math.Min(character.Energy, amount);

        character.Energy -= fromEnergy;
        character.ExtraEnergy -= amount - fromEnergy;

        return true;
    }

    public EnergyTrackerLevel GetEnergyLevel(EnergyCharacter character)
    {
        if (character.Energy >= _settings.EnergyCriticalThreshold)
        {
            return EnergyTrackerLevel.Critical;
        }

        return character.Energy >= _settings.EnergyWarningThreshold
            ? EnergyTrackerLevel.Warning
            : EnergyTrackerLevel.Normal;
    }

    public EnergyTrackerLevel GetNightmareTicketLevel(EnergyCharacter character)
    {
        // Critical when the next reset would overflow the cap, warning one reset earlier
        if (character.NightmareTickets + _settings.NightmareTicketsPerDay > _settings.MaxNightmareTickets)
        {
            return EnergyTrackerLevel.Critical;
        }

        return character.NightmareTickets + 2 * _settings.NightmareTicketsPerDay > _settings.MaxNightmareTickets
            ? EnergyTrackerLevel.Warning
            : EnergyTrackerLevel.Normal;
    }

    public void AcknowledgeAlerts(EnergyCharacter character)
    {
        character.EnergyAlertLevel = GetEnergyLevel(character);
        character.TicketAlertLevel = GetNightmareTicketLevel(character);
    }

    private int CountMarksBetween(TimeSpan[] dailyMarks, DateTime fromUtc, DateTime toUtc)
    {
        return MarksAfter(dailyMarks, fromUtc).TakeWhile(mark => mark <= toUtc).Count();
    }

    private IEnumerable<DateTime> MarksAfter(TimeSpan[] dailyMarks, DateTime fromUtc)
    {
        var localDate = TimeZoneInfo.ConvertTimeFromUtc(fromUtc, _timeZone).Date;

        while (true)
        {
            foreach (var mark in dailyMarks)
            {
                var local = DateTime.SpecifyKind(localDate + mark, DateTimeKind.Unspecified);

                if (_timeZone.IsInvalidTime(local))
                {
                    continue;
                }

                var utc = TimeZoneInfo.ConvertTimeToUtc(local, _timeZone);

                if (utc > fromUtc)
                {
                    yield return utc;
                }
            }

            localDate = localDate.AddDays(1);
        }
    }
}
