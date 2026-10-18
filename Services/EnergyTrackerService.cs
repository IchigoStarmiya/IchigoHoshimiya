using IchigoHoshimiya.Context;
using IchigoHoshimiya.Entities.General;
using IchigoHoshimiya.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IchigoHoshimiya.Services;

public class EnergyTrackerService(IchigoContext context, IEnergyTrackerCalculator calculator)
    : IEnergyTrackerService
{
    public async Task<EnergyCharacterResult> AddCharacterAsync(
        ulong userId,
        string name,
        EnergyCharacterUpdate values)
    {
        name = name.Trim();

        if (await FindCharacterAsync(userId, name) is not null)
        {
            return EnergyCharacterResult.NameTaken;
        }

        var now = DateTime.UtcNow;

        var character = new EnergyCharacter
        {
            UserId = userId,
            Name = name,
            ItemLevel = values.ItemLevel,
            IsMain = values.IsMain ?? false,
            Energy = values.Energy ?? 0,
            ExtraEnergy = values.ExtraEnergy ?? 0,
            NightmareTickets = values.NightmareTickets ?? 0,
            EnergyUpdatedAtUtc = now,
            TicketsUpdatedAtUtc = now
        };

        calculator.AcknowledgeAlerts(character);

        await context.EnergyCharacters.AddAsync(character);
        await context.SaveChangesAsync();

        return EnergyCharacterResult.Success;
    }

    public async Task<EnergyCharacterResult> UpdateCharacterAsync(
        ulong userId,
        string name,
        EnergyCharacterUpdate update)
    {
        var character = await FindCharacterAsync(userId, name.Trim());

        if (character is null)
        {
            return EnergyCharacterResult.NotFound;
        }

        var newName = update.NewName?.Trim();

        if (!string.IsNullOrEmpty(newName)
            && !newName.Equals(character.Name, StringComparison.OrdinalIgnoreCase)
            && await FindCharacterAsync(userId, newName) is not null)
        {
            return EnergyCharacterResult.NameTaken;
        }

        var now = DateTime.UtcNow;

        // Bank recharges owed so far, so untouched values don't lose them when the clocks reset below
        calculator.ApplyRecharges(character, now);

        if (update.Energy is { } energy)
        {
            character.Energy = energy;
            character.EnergyUpdatedAtUtc = now;
        }

        if (update.NightmareTickets is { } tickets)
        {
            character.NightmareTickets = tickets;
            character.TicketsUpdatedAtUtc = now;
        }

        if (update.ExtraEnergy is { } extraEnergy)
        {
            character.ExtraEnergy = extraEnergy;
        }

        if (update.ItemLevel is { } itemLevel)
        {
            character.ItemLevel = itemLevel;
        }

        if (update.IsMain is { } isMain)
        {
            character.IsMain = isMain;
        }

        if (!string.IsNullOrEmpty(newName))
        {
            character.Name = newName;
        }

        calculator.AcknowledgeAlerts(character);

        await context.SaveChangesAsync();

        return EnergyCharacterResult.Success;
    }

    public async Task<EnergyCharacterResult> RemoveCharacterAsync(ulong userId, string name)
    {
        var character = await FindCharacterAsync(userId, name.Trim());

        if (character is null)
        {
            return EnergyCharacterResult.NotFound;
        }

        context.EnergyCharacters.Remove(character);
        await context.SaveChangesAsync();

        return EnergyCharacterResult.Success;
    }

    public Task<IReadOnlyList<EnergyCharacter>> GetCharactersAsync(ulong userId)
    {
        return LoadCaughtUpAsync(context.EnergyCharacters.Where(c => c.UserId == userId));
    }

    public Task<IReadOnlyList<EnergyCharacter>> GetAllCharactersAsync()
    {
        return LoadCaughtUpAsync(context.EnergyCharacters);
    }

    public async Task<IReadOnlyList<EnergyDeductionResult>> DeductFromMainCharactersAsync(int amount)
    {
        var characters = await context.EnergyCharacters
                                      .Where(c => c.IsMain)
                                      .OrderBy(c => c.UserId)
                                      .ThenBy(c => c.Id)
                                      .ToListAsync();

        var now = DateTime.UtcNow;
        var results = new List<EnergyDeductionResult>();

        foreach (var character in characters)
        {
            calculator.ApplyRecharges(character, now);

            var energyBefore = character.Energy;
            var extraEnergyBefore = character.ExtraEnergy;
            var deducted = calculator.TryDeduct(character, amount);

            calculator.AcknowledgeAlerts(character);

            results.Add(new EnergyDeductionResult(character, energyBefore, extraEnergyBefore, deducted));
        }

        await context.SaveChangesAsync();

        return results;
    }

    private async Task<IReadOnlyList<EnergyCharacter>> LoadCaughtUpAsync(IQueryable<EnergyCharacter> query)
    {
        var characters = await query.OrderBy(c => c.UserId)
                                    .ThenBy(c => c.Id)
                                    .ToListAsync();

        var now = DateTime.UtcNow;

        // Catch up first so the list is accurate even if the background service hasn't run yet
        if (characters.Aggregate(false, (changed, c) => calculator.ApplyRecharges(c, now) || changed))
        {
            await context.SaveChangesAsync();
        }

        return characters;
    }

    private Task<EnergyCharacter?> FindCharacterAsync(ulong userId, string name)
    {
        var lowered = name.ToLower();

        return context.EnergyCharacters
                      .FirstOrDefaultAsync(c => c.UserId == userId && c.Name.ToLower() == lowered);
    }
}
