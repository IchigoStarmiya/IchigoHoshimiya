using IchigoHoshimiya.Services;

namespace IchigoHoshimiya.Interfaces;

public interface IHungerGamesService
{
    HungerGameState StartGame(ulong channelId);

    HungerGameState? GetGame(ulong channelId);

    EventResult AdvanceEvent(ulong channelId);

    bool EndGame(ulong channelId);
}
