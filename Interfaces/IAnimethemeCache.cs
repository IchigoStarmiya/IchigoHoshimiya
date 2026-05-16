using IchigoHoshimiya.Entities.Animethemes;

namespace IchigoHoshimiya.Interfaces;

public sealed record AnimethemeCacheSnapshot(
    IReadOnlyList<AnimeThemeEntry> Entries,
    IReadOnlyDictionary<ulong, IReadOnlyList<string>> SynonymsByAnimeId);

public interface IAnimethemeCache
{
    Task<AnimethemeCacheSnapshot> GetAsync(CancellationToken cancellationToken = default);

    Task RefreshAsync(CancellationToken cancellationToken = default);
}
