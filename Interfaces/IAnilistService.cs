using NetCord.Rest;

namespace IchigoHoshimiya.Interfaces;

public interface IAnilistService
{
    Task<EmbedProperties> GetAnimeAsync(string query);
    Task<EmbedProperties> GetMangaAsync(string query);
    Task<EmbedProperties> GetUserAsync(string name);
}
