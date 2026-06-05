using System.Collections.Concurrent;
using IchigoHoshimiya.Helpers;
using IchigoHoshimiya.Interfaces;

namespace IchigoHoshimiya.Services;

public enum Phase
{
    Day,
    Night
}

public sealed class Tribute(string name, string iconPath)
{
    public string Name { get; } = name;
    public string IconPath { get; } = iconPath;
    public bool Alive { get; set; } = true;
}

public sealed class HungerGameState
{
    public List<Tribute> Tributes { get; init; } = [];
    public int Day { get; set; }
    public Phase CurrentPhase { get; set; } = Phase.Night;
    public Queue<Tribute> PhasePool { get; set; } = new();
    public double DeathProb { get; set; }
    public bool IsOver { get; set; }
}

public sealed record EventParticipant(string Name, string IconPath, bool Died);

public sealed record EventResult(
    int Day,
    Phase Phase,
    bool PhaseStarted,
    string Line,
    IReadOnlyList<EventParticipant> Participants,
    IReadOnlyList<string> Survivors,
    string? Winner,
    string? WinnerIconPath,
    bool IsOver);

public sealed class HungerGamesService : IHungerGamesService
{
    private static readonly HashSet<string> ImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp" };

    private readonly ConcurrentDictionary<ulong, HungerGameState> _games = new();
    private readonly string _iconsFolder = Path.Combine(AppContext.BaseDirectory, "Icons");

    public HungerGameState StartGame(ulong channelId)
    {
        if (!Directory.Exists(_iconsFolder))
        {
            throw new InvalidOperationException($"Icons folder not found at `{_iconsFolder}`.");
        }

        var tributes = Directory.EnumerateFiles(_iconsFolder)
            .Where(f => ImageExtensions.Contains(Path.GetExtension(f)))
            .Select(f => new Tribute(Path.GetFileNameWithoutExtension(f)!, f))
            .Where(t => !string.IsNullOrWhiteSpace(t.Name))
            .ToList();

        if (tributes.Count < 2)
        {
            throw new InvalidOperationException("Need at least 2 image icons in the Icons folder to start.");
        }

        var game = new HungerGameState { Tributes = tributes };
        _games[channelId] = game;
        return game;
    }

    public HungerGameState? GetGame(ulong channelId)
    {
        return _games.GetValueOrDefault(channelId);
    }

    public EventResult AdvanceEvent(ulong channelId)
    {
        if (!_games.TryGetValue(channelId, out var game))
        {
            throw new InvalidOperationException("No active Hunger Games in this channel. Start one with `;hungergames`.");
        }

        if (game.IsOver)
        {
            throw new InvalidOperationException("This game is already over.");
        }

        var phaseStarted = false;
        if (game.PhasePool.Count == 0)
        {
            var nextPhase = game.CurrentPhase == Phase.Day ? Phase.Night : Phase.Day;
            game.CurrentPhase = nextPhase;
            if (nextPhase == Phase.Day)
            {
                game.Day++;
            }

            var alive = game.Tributes
                .Where(t => t.Alive)
                .OrderBy(_ => Random.Shared.Next())
                .ToList();

            game.PhasePool = new Queue<Tribute>(alive);
            // Per-event death probability. 2 alive → 5%, 5 → ~30%, 10+ → 80%.
            game.DeathProb = Math.Clamp((alive.Count - 2) * 0.10, 0.05, 0.80);
            phaseStarted = true;
        }

        var events = game.CurrentPhase == Phase.Day ? HungerGameEvents.Day : HungerGameEvents.Night;
        var totalAlive = game.Tributes.Count(t => t.Alive);
        var allowDeath = Random.Shared.NextDouble() < game.DeathProb;

        // Hard constraints: fits remaining pool, and never wipes the arena (a winner must survive).
        var eligible = events
            .Where(e => e.Participants <= game.PhasePool.Count && e.Deaths.Length < totalAlive)
            .ToList();

        if (eligible.Count == 0)
        {
            throw new InvalidOperationException("No event fits the current pool size.");
        }

        // Soft preference: respect the death-prob gate when a non-fatal option exists.
        var preferred = allowDeath
            ? eligible
            : eligible.Where(e => e.Deaths.Length == 0).ToList();
        if (preferred.Count == 0)
        {
            preferred = eligible;
        }

        var ev = preferred[Random.Shared.Next(preferred.Count)];

        var picked = new Tribute[ev.Participants];
        for (var i = 0; i < ev.Participants; i++)
        {
            picked[i] = game.PhasePool.Dequeue();
        }

        var line = ev.Template;
        for (var i = 0; i < picked.Length; i++)
        {
            line = line.Replace("{" + i + "}", picked[i].Name);
        }

        var deathIndices = new HashSet<int>(ev.Deaths);
        var participants = picked
            .Select((t, i) => new EventParticipant(t.Name, t.IconPath, deathIndices.Contains(i)))
            .ToList();

        foreach (var idx in ev.Deaths)
        {
            picked[idx].Alive = false;
        }

        var survivorTributes = game.Tributes.Where(t => t.Alive).ToList();
        var survivors = survivorTributes.Select(t => t.Name).ToList();

        string? winner = null;
        string? winnerIcon = null;

        if (survivorTributes.Count <= 1)
        {
            game.IsOver = true;
            winner = survivorTributes.FirstOrDefault()?.Name;
            winnerIcon = survivorTributes.FirstOrDefault()?.IconPath;
        }

        if (game.IsOver)
        {
            _games.TryRemove(channelId, out _);
        }

        return new EventResult(
            game.Day,
            game.CurrentPhase,
            phaseStarted,
            line,
            participants,
            survivors,
            winner,
            winnerIcon,
            game.IsOver);
    }

    public bool EndGame(ulong channelId)
    {
        return _games.TryRemove(channelId, out _);
    }
}
