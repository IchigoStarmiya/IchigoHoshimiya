using NetCord.Rest;

namespace IchigoHoshimiya.Interfaces;

public interface IAnimethemeService
{
    public Task<string> GetAnimetheme(string query, string? slug);

    public Task<EmbedProperties> GetAllAnimethemes(string query, string? slug);
}