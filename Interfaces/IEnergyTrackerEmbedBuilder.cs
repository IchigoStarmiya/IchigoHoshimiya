using IchigoHoshimiya.Entities.General;
using NetCord.Rest;

namespace IchigoHoshimiya.Interfaces;

public interface IEnergyTrackerEmbedBuilder
{
    IReadOnlyList<EmbedProperties> BuildList(string ownerName, IReadOnlyList<EnergyCharacter> characters);

    IReadOnlyList<EmbedProperties> BuildOverview(IReadOnlyList<EnergyCharacter> characters);

    IReadOnlyList<EmbedProperties> BuildDeductionSummary(int amount, IReadOnlyList<EnergyDeductionResult> results);
}
