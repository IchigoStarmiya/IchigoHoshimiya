using System.Text;
using FuzzySharp;
using IchigoHoshimiya.DTO;
using IchigoHoshimiya.Entities.Animethemes;
using IchigoHoshimiya.Helpers;
using IchigoHoshimiya.Interfaces;
using NetCord.Rest;

namespace IchigoHoshimiya.Services;

public class AnimethemeService(IAnimethemeCache cache) : IAnimethemeService
{
    public async Task<EmbedProperties> GetAllAnimethemes(string query, string? slug)
    {
        var fuzzyMatchesDto = await GetFuzzyMatches(query, 10, slug);

        if (fuzzyMatchesDto.Count == 0)
        {
            return new EmbedProperties()
               .WithTitle("No matches found for your query");
        }

        var embedDescriptionBuilder = new StringBuilder();

        for (var i = 0; i < fuzzyMatchesDto.Count; i++)
        {
            var match = fuzzyMatchesDto[i];

            embedDescriptionBuilder.AppendLine(
                $"{i + 1}. [{match.Anime} {match.Slug} - {match.Theme}]({match.Link})"
            );
        }

        return EmbedHelper.Build("Your search results", embedDescriptionBuilder.ToString());
    }

    public async Task<string> GetAnimetheme(string query, string? slug)
    {
        var fuzzyMatchesDto = await GetFuzzyMatches(query, 5, slug);

        if (fuzzyMatchesDto.Count == 0)
        {
            return "No matches found for your query";
        }

        var firstMatch = fuzzyMatchesDto[0];

        return $"{firstMatch.Anime} {firstMatch.Slug} - {firstMatch.Theme}\n{firstMatch.Link}";
    }

    private static List<AnimethemeDto> ToDto(List<AnimeThemeEntry> themes)
    {
        return themes.Select(theme =>
                      {
                          var highestResVideo = theme.AnimeThemeEntryVideos
                                                     .Select(entryVideo => entryVideo.Video)
                                                     .OrderByDescending(video => video.Resolution)
                                                     .FirstOrDefault();

                          return new AnimethemeDto
                          {
                              Anime = theme.Theme.Anime.Name,
                              Slug = theme.Theme.Slug,
                              Theme = theme.Theme.Song!.Title!,
                              Link = $"https://v.animethemes.moe/{highestResVideo!.Basename}"
                          };
                      })
                     .ToList();
    }

    // Cute to have some code debt so let's keep it this way for now since it's still kinda "in beta"
    // Clean up in the future for sure copium
    private async Task<List<AnimethemeDto>> GetFuzzyMatches(string query, int count, string? slug)
    {
        var snapshot = await cache.GetAsync();

        IEnumerable<AnimeThemeEntry> candidates = snapshot.Entries;

        if (!string.IsNullOrWhiteSpace(slug))
        {
            candidates = candidates.Where(e => e.Theme.Slug == slug);
        }

        var synonymsByAnimeId = snapshot.SynonymsByAnimeId;

        var normalizedQuery = Normalize(query);
        var queryTokens = normalizedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var multiToken = queryTokens.Length > 1;
        var firstToken = queryTokens.FirstOrDefault() ?? normalizedQuery;

        var scored = candidates.AsParallel()
                               .Select(e =>
                                {
                                    var animeName = Normalize(e.Theme.Anime.Name);

                                    var synonyms = synonymsByAnimeId.TryGetValue(
                                        e.Theme.AnimeId,
                                        out var rawSynonyms)
                                        ? rawSynonyms.Select(s => Normalize(s)).ToList()
                                        : new List<string>();

                                    var themeTitle = Normalize(e.Theme.Song?.Title ?? "");

                                    var animeNameScore = Math.Max(
                                        Fuzz.TokenSortRatio(normalizedQuery, animeName),
                                        Fuzz.TokenSetRatio(normalizedQuery, animeName)
                                    );

                                    var synonymScore = synonyms.Count > 0
                                        ? synonyms.Max(s => Math.Max(
                                            Fuzz.TokenSortRatio(normalizedQuery, s),
                                            Fuzz.TokenSetRatio(normalizedQuery, s)
                                        ))
                                        : 0;

                                    var bestAnimeScore = Math.Max(animeNameScore, synonymScore);

                                    var themeScore1 = Fuzz.TokenSetRatio(normalizedQuery, themeTitle);
                                    var themeScore2 = Fuzz.PartialRatio(normalizedQuery, themeTitle);
                                    var bestThemeScore = Math.Max(themeScore1, themeScore2);

                                    var firstTokenAnimeScore = Math.Max(
                                        Fuzz.PartialRatio(firstToken, animeName),
                                        synonyms.Count > 0
                                            ? synonyms.Max(s => Fuzz.PartialRatio(firstToken, s))
                                            : 0
                                    );

                                    var looksLikeThemeQuery = bestAnimeScore < 50 && bestThemeScore >= 50;

                                    var weightedScore = looksLikeThemeQuery
                                        ? 0.25 * bestAnimeScore + 0.75 * bestThemeScore
                                        : multiToken
                                            ? 0.75 * bestAnimeScore + 0.25 * bestThemeScore
                                            : 0.5 * bestAnimeScore + 0.5 * bestThemeScore;

                                    if (string.Equals(
                                            themeTitle,
                                            normalizedQuery,
                                            StringComparison.OrdinalIgnoreCase) ||
                                        synonyms.Any(s => string.Equals(
                                            s,
                                            normalizedQuery,
                                            StringComparison.OrdinalIgnoreCase)) ||
                                        string.Equals(animeName, normalizedQuery, StringComparison.OrdinalIgnoreCase))
                                    {
                                        weightedScore += 15;
                                    }

                                    if (animeName.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                                    {
                                        weightedScore += 30;
                                    }
                                    else if (animeName.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                                    {
                                        weightedScore += 15;
                                    }

                                    if (themeTitle.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                                    {
                                        weightedScore += 20;
                                    }
                                    else if (themeTitle.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                                    {
                                        weightedScore += 10;
                                    }

                                    var allTokensInAnime = queryTokens.All(t =>
                                        animeName.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                                        synonyms.Any(s => s.Contains(t, StringComparison.OrdinalIgnoreCase))
                                    );

                                    if (allTokensInAnime)
                                    {
                                        weightedScore += 20;
                                    }

                                    return new
                                    {
                                        Entry = e,
                                        Score = weightedScore,
                                        AnimeScore = bestAnimeScore,
                                        ThemeScore = bestThemeScore,
                                        FirstTokenAnimeScore = firstTokenAnimeScore
                                    };
                                })
                               .Where(x =>
                                {
                                    if (!multiToken)
                                    {
                                        return x.AnimeScore >= 60 || x.ThemeScore >= 60;
                                    }

                                    if (x.FirstTokenAnimeScore >= 60)
                                    {
                                        return x.AnimeScore >= 50;
                                    }

                                    return x.ThemeScore >= 55;
                                })
                               .OrderByDescending(x => x.Score)
                               .GroupBy(x => x.Entry.Theme.ThemeId)
                               .Select(g => g.First())
                               .Take(count)
                               .ToList();


        var fuzzyMatches = scored
                          .OrderByDescending(x => x.Score)
                          .Take(count)
                          .Select(x => x.Entry)
                          .ToList();

        return ToDto(fuzzyMatches);

        string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "";
            }

            var lowered = input.ToLowerInvariant();

            var chars = lowered.Select(c =>
                                    char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)
                                        ? c
                                        : ' '
                                )
                               .ToArray();

            return string.Join(
                " ",
                new string(chars)
                   .Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}