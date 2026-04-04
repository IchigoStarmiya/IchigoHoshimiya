using IchigoHoshimiya.Handlers;
using IchigoHoshimiya.Interfaces;
using JetBrains.Annotations;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace IchigoHoshimiya.Modules.SlashCommands;

[UsedImplicitly]
public class AnilistSlashCommandModule(IAnilistService anilistService)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("anime", "Search for an anime on AniList.")]
    [UsedImplicitly]
    public async Task GetAnime(
        [SlashCommandParameter(Name = "query", Description = "Anime title to search for")]
        string query)
    {
        var command = new GenericDeferredSlashCommandHandlerForEmbed(Context, () => anilistService.GetAnimeAsync(query));

        await command.ExecuteAsync();
    }

    [SlashCommand("manga", "Search for a manga on AniList.")]
    [UsedImplicitly]
    public async Task GetManga(
        [SlashCommandParameter(Name = "query", Description = "Manga title to search for")]
        string query)
    {
        var command = new GenericDeferredSlashCommandHandlerForEmbed(Context, () => anilistService.GetMangaAsync(query));

        await command.ExecuteAsync();
    }

    [SlashCommand("user", "Look up an AniList user profile.")]
    [UsedImplicitly]
    public async Task GetUser(
        [SlashCommandParameter(Name = "name", Description = "AniList username to look up")]
        string name)
    {
        var command = new GenericDeferredSlashCommandHandlerForEmbed(Context, () => anilistService.GetUserAsync(name));

        await command.ExecuteAsync();
    }
}
