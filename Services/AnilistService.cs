using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using IchigoHoshimiya.Helpers;
using IchigoHoshimiya.Interfaces;
using NetCord.Rest;

namespace IchigoHoshimiya.Services;

public partial class AnilistService(HttpClient httpClient) : IAnilistService
{
    private const string AnilistEndpoint = "https://graphql.anilist.co";

    private const string AnimeQuery = """
        query ($search: String) {
          results: Media(type: ANIME, sort: SEARCH_MATCH, search: $search) {
            title { romaji }
            coverImage { large }
            siteUrl
            averageScore
            description
            episodes
            format
            genres
            idMal
            nextAiringEpisode { episode timeUntilAiring }
            rankings { context rank }
            source
            status
            startDate { year month day }
            endDate { year month day }
          }
        }
        """;

    private const string MangaQuery = """
        query ($search: String) {
          results: Media(type: MANGA, sort: SEARCH_MATCH, search: $search) {
            title { romaji }
            coverImage { large }
            siteUrl
            averageScore
            description
            chapters
            format
            genres
            idMal
            rankings { context rank }
            source
            status
            startDate { year month day }
            endDate { year month day }
          }
        }
        """;

    private const string UserQuery = """
        query ($name: String) {
          User(search: $name) {
            name
            siteUrl
            avatar { large }
            statistics {
              anime { count meanScore }
              manga { count meanScore }
            }
            favourites(page: 1) {
              anime(page: 1, perPage: 5) { nodes { title { romaji } siteUrl } }
              manga(page: 1, perPage: 5) { nodes { title { romaji } siteUrl } }
              characters(page: 1, perPage: 5) {
                nodes {
                  name { full }
                  siteUrl
                  media(sort: [POPULARITY_DESC], perPage: 1, page: 1) { nodes { title { romaji } } }
                }
              }
              staff(page: 1, perPage: 5) { nodes { name { full } siteUrl } }
            }
          }
        }
        """;

    public Task<EmbedProperties> GetAnimeAsync(string query) =>
        GetMediaAsync(query, AnimeQuery, isManga: false);

    public Task<EmbedProperties> GetMangaAsync(string query) =>
        GetMediaAsync(query, MangaQuery, isManga: true);

    public async Task<EmbedProperties> GetUserAsync(string name)
    {
        var response = await httpClient.PostAsJsonAsync(AnilistEndpoint, new
        {
            query = UserQuery,
            variables = new { name }
        });

        if (!response.IsSuccessStatusCode)
            return EmbedHelper.Build("Failed to reach AniList. Please try again later.");

        var result = await response.Content.ReadFromJsonAsync<AnilistUserResponse>();
        var user = result?.Data?.User;

        if (user is null)
            return EmbedHelper.Build("No user found for your query.");

        var fields = new List<EmbedFieldProperties>
        {
            new() { Name = "Total Anime", Value = $"{user.Statistics?.Anime?.Count ?? 0}", Inline = true }
        };

        if (user.Statistics?.Anime?.MeanScore is > 0)
            fields.Add(new EmbedFieldProperties { Name = "Mean Score (Anime)", Value = $"{user.Statistics.Anime.MeanScore}" });

        fields.Add(new EmbedFieldProperties { Name = "Total Manga", Value = $"{user.Statistics?.Manga?.Count ?? 0}" });

        if (user.Statistics?.Manga?.MeanScore is > 0)
            fields.Add(new EmbedFieldProperties { Name = "Mean Score (Manga)", Value = $"{user.Statistics.Manga.MeanScore}" });

        var animeFavourites = BuildMediaFavourites(user.Favourites?.Anime?.Nodes);
        var mangaFavourites = BuildMediaFavourites(user.Favourites?.Manga?.Nodes);
        var charFavourites = BuildCharacterFavourites(user.Favourites?.Characters?.Nodes);
        var staffFavourites = BuildStaffFavourites(user.Favourites?.Staff?.Nodes);

        if (!string.IsNullOrEmpty(animeFavourites))
            fields.Add(new EmbedFieldProperties { Name = "Favourite Anime", Value = animeFavourites });

        if (!string.IsNullOrEmpty(mangaFavourites))
            fields.Add(new EmbedFieldProperties { Name = "Favourite Manga", Value = mangaFavourites });

        if (!string.IsNullOrEmpty(charFavourites))
            fields.Add(new EmbedFieldProperties { Name = "Favourite Characters", Value = charFavourites });

        if (!string.IsNullOrEmpty(staffFavourites))
            fields.Add(new EmbedFieldProperties { Name = "Favourite Staff", Value = staffFavourites });

        return new EmbedProperties
        {
            Author = new EmbedAuthorProperties { Name = user.Name, Url = user.SiteUrl, IconUrl = user.Avatar?.Large },
            Thumbnail = new EmbedThumbnailProperties(user.Avatar?.Large),
            Color = EmbedHelper.Build().Color,
            Fields = fields
        };

        static string BuildMediaFavourites(AnilistFavouriteMediaNode[]? nodes)
        {
            if (nodes is not { Length: > 0 }) return string.Empty;
            return string.Join("\n", nodes.Select(n => $"[{n.Title?.Romaji}]({n.SiteUrl})"));
        }

        static string BuildCharacterFavourites(AnilistFavouriteCharacterNode[]? nodes)
        {
            if (nodes is not { Length: > 0 }) return string.Empty;
            return string.Join("\n", nodes.Select(n =>
            {
                var media = n.Media?.Nodes?.FirstOrDefault();
                return media is not null
                    ? $"[{n.Name?.Full}]({n.SiteUrl}) ({media.Title?.Romaji})"
                    : $"[{n.Name?.Full}]({n.SiteUrl})";
            }));
        }

        static string BuildStaffFavourites(AnilistFavouriteStaffNode[]? nodes)
        {
            if (nodes is not { Length: > 0 }) return string.Empty;
            return string.Join("\n", nodes.Select(n => $"[{n.Name?.Full}]({n.SiteUrl})"));
        }
    }

    private async Task<EmbedProperties> GetMediaAsync(string query, string gqlQuery, bool isManga)
    {
        var response = await httpClient.PostAsJsonAsync(AnilistEndpoint, new
        {
            query = gqlQuery,
            variables = new { search = query }
        });

        if (!response.IsSuccessStatusCode)
            return EmbedHelper.Build("Failed to reach AniList. Please try again later.");

        var result = await response.Content.ReadFromJsonAsync<AnilistResponse>();
        var media = result?.Data?.Results;

        if (media is null)
            return EmbedHelper.Build("No results found for your query.");

        var rank = media.Rankings?.FirstOrDefault(r =>
            string.Equals(r.Context, "most popular all time", StringComparison.OrdinalIgnoreCase));

        var countFieldName = isManga ? "Chapters" : "Episodes";
        var countFieldValue = isManga
            ? (media.Chapters.HasValue ? $"{media.Chapters}" : "Unknown")
            : (media.Episodes.HasValue ? $"{media.Episodes}" : "Unknown");

        var fields = new List<EmbedFieldProperties>
        {
            new() { Name = "Average Score", Value = media.AverageScore.HasValue ? $"{media.AverageScore}%" : "-", Inline = true },
            new() { Name = "Ranking", Value = rank is not null ? $"#{rank.Rank}" : "Unknown", Inline = true },
            new() { Name = "Format", Value = media.Format ?? "Unknown", Inline = true },
            new() { Name = "Source", Value = media.Source ?? "Unknown", Inline = true },
            new() { Name = countFieldName, Value = countFieldValue, Inline = true },
            new() { Name = "Status", Value = media.Status ?? "Unknown", Inline = true },
            new() { Name = "Start Date", Value = FormatDate(media.StartDate?.Year, media.StartDate?.Month, media.StartDate?.Day), Inline = true },
            new() { Name = "End Date", Value = FormatDate(media.EndDate?.Year, media.EndDate?.Month, media.EndDate?.Day), Inline = true },
            new() { Name = "Genres", Value = media.Genres is { Length: > 0 } ? string.Join("\n", media.Genres) : "Unknown", Inline = true },
        };

        if (!isManga && media.NextAiringEpisode is not null)
        {
            fields.Add(new EmbedFieldProperties
            {
                Name = $"Next Episode (Ep {media.NextAiringEpisode.Episode})",
                Value = FormatTimeUntilAiring(media.NextAiringEpisode.TimeUntilAiring)
            });
        }

        fields.Add(new EmbedFieldProperties
        {
            Name = "Description",
            Value = FormatDescription(media.Description)
        });

        var malPath = isManga ? "manga" : "anime";
        var malUrl = media.IdMal.HasValue
            ? $"https://myanimelist.net/{malPath}/{media.IdMal}"
            : null;

        var links = malUrl is not null
            ? $"[AniList]({media.SiteUrl}) | [MyAnimeList]({malUrl})"
            : $"[AniList]({media.SiteUrl})";

        return new EmbedProperties
        {
            Title = media.Title?.Romaji,
            Description = links,
            Color = EmbedHelper.Build().Color,
            Thumbnail = new EmbedThumbnailProperties(media.CoverImage?.Large),
            Fields = fields
        };
    }

    private static string FormatDate(int? year, int? month, int? day)
    {
        if (month is null) return "Unknown";
        return $"{day}/{month}/{year}";
    }

    private static string FormatTimeUntilAiring(int totalSeconds)
    {
        var days = totalSeconds / 86400;
        var hours = totalSeconds % 86400 / 3600;
        var minutes = totalSeconds % 3600 / 60;

        if (days == 0 && hours == 0 && minutes == 0) return "Literal seconds away";
        if (days == 0 && hours == 0) return $"{minutes} minutes";
        if (days == 0) return $"{hours} hours {minutes} minutes";
        return $"{days} days {hours} hours {minutes} minutes";
    }

    private static string FormatDescription(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return "*No description provided*";

        var text = BrTagRegex().Replace(html, "\n");
        text = HtmlTagRegex().Replace(text, "");
        text = text
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&quot;", "\"")
            .Replace("&#039;", "'")
            .Trim();

        if (text.Length >= 1020)
            text = text[..1020] + "...";

        return text;
    }

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex BrTagRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();
}

file sealed class AnilistResponse
{
    [JsonPropertyName("data")]
    public AnilistData? Data { get; init; }
}

file sealed class AnilistData
{
    [JsonPropertyName("results")]
    public AnilistMedia? Results { get; init; }
}

file sealed class AnilistMedia
{
    [JsonPropertyName("title")]
    public AnilistTitle? Title { get; init; }

    [JsonPropertyName("coverImage")]
    public AnilistCoverImage? CoverImage { get; init; }

    [JsonPropertyName("siteUrl")]
    public string? SiteUrl { get; init; }

    [JsonPropertyName("averageScore")]
    public int? AverageScore { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("episodes")]
    public int? Episodes { get; init; }

    [JsonPropertyName("chapters")]
    public int? Chapters { get; init; }

    [JsonPropertyName("format")]
    public string? Format { get; init; }

    [JsonPropertyName("genres")]
    public string[]? Genres { get; init; }

    [JsonPropertyName("idMal")]
    public int? IdMal { get; init; }

    [JsonPropertyName("nextAiringEpisode")]
    public AnilistNextAiringEpisode? NextAiringEpisode { get; init; }

    [JsonPropertyName("rankings")]
    public AnilistRanking[]? Rankings { get; init; }

    [JsonPropertyName("source")]
    public string? Source { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("startDate")]
    public AnilistDate? StartDate { get; init; }

    [JsonPropertyName("endDate")]
    public AnilistDate? EndDate { get; init; }
}

file sealed class AnilistTitle
{
    [JsonPropertyName("romaji")]
    public string? Romaji { get; init; }
}

file sealed class AnilistCoverImage
{
    [JsonPropertyName("large")]
    public string? Large { get; init; }
}

file sealed class AnilistDate
{
    [JsonPropertyName("year")]
    public int? Year { get; init; }

    [JsonPropertyName("month")]
    public int? Month { get; init; }

    [JsonPropertyName("day")]
    public int? Day { get; init; }
}

file sealed class AnilistNextAiringEpisode
{
    [JsonPropertyName("episode")]
    public int Episode { get; init; }

    [JsonPropertyName("timeUntilAiring")]
    public int TimeUntilAiring { get; init; }
}

file sealed class AnilistRanking
{
    [JsonPropertyName("context")]
    public string? Context { get; init; }

    [JsonPropertyName("rank")]
    public int Rank { get; init; }
}

file sealed class AnilistUserResponse
{
    [JsonPropertyName("data")]
    public AnilistUserData? Data { get; init; }
}

file sealed class AnilistUserData
{
    [JsonPropertyName("User")]
    public AnilistUser? User { get; init; }
}

file sealed class AnilistUser
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("siteUrl")]
    public string? SiteUrl { get; init; }

    [JsonPropertyName("avatar")]
    public AnilistUserAvatar? Avatar { get; init; }

    [JsonPropertyName("statistics")]
    public AnilistUserStatistics? Statistics { get; init; }

    [JsonPropertyName("favourites")]
    public AnilistFavourites? Favourites { get; init; }
}

file sealed class AnilistUserAvatar
{
    [JsonPropertyName("large")]
    public string? Large { get; init; }
}

file sealed class AnilistUserStatistics
{
    [JsonPropertyName("anime")]
    public AnilistMediaStats? Anime { get; init; }

    [JsonPropertyName("manga")]
    public AnilistMediaStats? Manga { get; init; }
}

file sealed class AnilistMediaStats
{
    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("meanScore")]
    public double MeanScore { get; init; }
}

file sealed class AnilistFavourites
{
    [JsonPropertyName("anime")]
    public AnilistFavouriteMediaConnection? Anime { get; init; }

    [JsonPropertyName("manga")]
    public AnilistFavouriteMediaConnection? Manga { get; init; }

    [JsonPropertyName("characters")]
    public AnilistFavouriteCharacterConnection? Characters { get; init; }

    [JsonPropertyName("staff")]
    public AnilistFavouriteStaffConnection? Staff { get; init; }
}

file sealed class AnilistFavouriteMediaConnection
{
    [JsonPropertyName("nodes")]
    public AnilistFavouriteMediaNode[]? Nodes { get; init; }
}

file sealed class AnilistFavouriteMediaNode
{
    [JsonPropertyName("title")]
    public AnilistTitle? Title { get; init; }

    [JsonPropertyName("siteUrl")]
    public string? SiteUrl { get; init; }
}

file sealed class AnilistFavouriteCharacterConnection
{
    [JsonPropertyName("nodes")]
    public AnilistFavouriteCharacterNode[]? Nodes { get; init; }
}

file sealed class AnilistFavouriteCharacterNode
{
    [JsonPropertyName("name")]
    public AnilistCharacterName? Name { get; init; }

    [JsonPropertyName("siteUrl")]
    public string? SiteUrl { get; init; }

    [JsonPropertyName("media")]
    public AnilistFavouriteMediaConnection? Media { get; init; }
}

file sealed class AnilistCharacterName
{
    [JsonPropertyName("full")]
    public string? Full { get; init; }
}

file sealed class AnilistFavouriteStaffConnection
{
    [JsonPropertyName("nodes")]
    public AnilistFavouriteStaffNode[]? Nodes { get; init; }
}

file sealed class AnilistFavouriteStaffNode
{
    [JsonPropertyName("name")]
    public AnilistCharacterName? Name { get; init; }

    [JsonPropertyName("siteUrl")]
    public string? SiteUrl { get; init; }
}
