using IchigoHoshimiya.Interfaces;
using JetBrains.Annotations;
using NetCord.Rest;
using NetCord.Services.Commands;

namespace IchigoHoshimiya.Modules.TextCommands;

[UsedImplicitly]
public class AnilistTextCommandModule(IAnilistService anilistService)
    : CommandModule<CommandContext>
{
    [Command("anime")]
    [UsedImplicitly]
    public async Task GetAnime([CommandParameter(Remainder = true)] string query)
    {
        var embed = await anilistService.GetAnimeAsync(query);

        await Context.Message.SendAsync(new MessageProperties().WithEmbeds([embed]));
    }

    [Command("manga")]
    [UsedImplicitly]
    public async Task GetManga([CommandParameter(Remainder = true)] string query)
    {
        var embed = await anilistService.GetMangaAsync(query);

        await Context.Message.SendAsync(new MessageProperties().WithEmbeds([embed]));
    }

    [Command("user")]
    [UsedImplicitly]
    public async Task GetUser([CommandParameter(Remainder = true)] string name)
    {
        var embed = await anilistService.GetUserAsync(name);

        await Context.Message.SendAsync(new MessageProperties().WithEmbeds([embed]));
    }
}
