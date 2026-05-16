using System.Diagnostics;
using IchigoHoshimiya.Context;
using IchigoHoshimiya.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IchigoHoshimiya.Services;

public sealed class AnimethemeCache(
    IServiceScopeFactory scopeFactory,
    ILogger<AnimethemeCache> logger) : IAnimethemeCache
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private AnimethemeCacheSnapshot? _snapshot;

    public async Task<AnimethemeCacheSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_snapshot is { } snapshot)
        {
            return snapshot;
        }

        await RefreshAsync(cancellationToken);

        return _snapshot!;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken);

        try
        {
            logger.LogInformation("Rebuilding animetheme cache...");

            var stopwatch = Stopwatch.StartNew();

            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AnimethemesDbContext>();

            var entries = await dbContext.AnimeThemeEntries
                                         .AsNoTracking()
                                         .AsSplitQuery()
                                         .Include(e => e.Theme)
                                         .ThenInclude(t => t.Song)
                                         .Include(e => e.Theme)
                                         .ThenInclude(t => t.Anime)
                                         .Include(e => e.AnimeThemeEntryVideos)
                                         .ThenInclude(v => v.Video)
                                         .Where(e => e.Theme.Song != null)
                                         .Where(e => e.AnimeThemeEntryVideos.Any())
                                         .ToListAsync(cancellationToken);

            var animeIds = entries.Select(e => e.Theme.AnimeId).Distinct().ToList();

            var synonyms = await dbContext.Synonyms
                                          .AsNoTracking()
                                          .Where(s => s.SynonymableType == "anime" &&
                                                      animeIds.Contains(s.SynonymableId))
                                          .ToListAsync(cancellationToken);

            var synonymsByAnimeId = synonyms
                                   .GroupBy(s => s.SynonymableId)
                                   .ToDictionary(
                                        g => g.Key,
                                        g => (IReadOnlyList<string>)g.Select(s => s.Text).ToList());

            _snapshot = new AnimethemeCacheSnapshot(entries, synonymsByAnimeId);

            logger.LogInformation(
                "Animetheme cache rebuilt: {Count} entries in {Ms}ms.",
                entries.Count,
                stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
