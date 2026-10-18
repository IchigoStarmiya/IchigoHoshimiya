using IchigoHoshimiya.Entities.General;

namespace IchigoHoshimiya.Interfaces;

public record EnergyCharacterUpdate(
    int? Energy = null,
    int? ExtraEnergy = null,
    int? NightmareTickets = null,
    int? ItemLevel = null,
    bool? IsMain = null,
    string? NewName = null);

public record EnergyDeductionResult(
    EnergyCharacter Character,
    int EnergyBefore,
    int ExtraEnergyBefore,
    bool Deducted);

public enum EnergyCharacterResult
{
    Success,
    NotFound,
    NameTaken
}

public interface IEnergyTrackerService
{
    Task<EnergyCharacterResult> AddCharacterAsync(ulong userId, string name, EnergyCharacterUpdate values);

    Task<EnergyCharacterResult> UpdateCharacterAsync(ulong userId, string name, EnergyCharacterUpdate update);

    Task<EnergyCharacterResult> RemoveCharacterAsync(ulong userId, string name);

    Task<IReadOnlyList<EnergyCharacter>> GetCharactersAsync(ulong userId);

    Task<IReadOnlyList<EnergyCharacter>> GetAllCharactersAsync();

    Task<IReadOnlyList<EnergyDeductionResult>> DeductFromMainCharactersAsync(int amount);
}
