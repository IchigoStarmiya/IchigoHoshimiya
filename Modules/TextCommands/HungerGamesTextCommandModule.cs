using System.Text;
using IchigoHoshimiya.Helpers;
using IchigoHoshimiya.Interfaces;
using IchigoHoshimiya.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using NetCord.Rest;
using NetCord.Services.Commands;

namespace IchigoHoshimiya.Modules.TextCommands;

[UsedImplicitly]
public class HungerGamesTextCommandModule(
    IHungerGamesService gamesService,
    IClient ichigoClient,
    IConfiguration configuration) : CommandModule<CommandContext>
{
    [Command("hungergames")]
    [UsedImplicitly]
    public async Task StartGame()
    {
        if (!IsOwner())
        {
            return;
        }

        HungerGameState game;
        try
        {
            game = gamesService.StartGame(Context.Message.ChannelId);
        }
        catch (InvalidOperationException ex)
        {
            await ichigoClient.SendMessageAsync(Context.Message.ChannelId, ex.Message);
            return;
        }

        var roster = string.Join("\n", game.Tributes.Select(t => $"• **{t.Name}**"));
        var embed = EmbedHelper.Build(
            $"The Hunger Games Begin — {game.Tributes.Count} Tributes",
            $"{roster}\n\nUse `;hgnext` to resolve the next event.");
        embed.Image = new EmbedImageProperties("attachment://roster.png");

        await using var rosterImage = IconCompositor.ComposeRoster(
            game.Tributes.Select(t => t.IconPath).ToList());
        var attachment = new AttachmentProperties("roster.png", rosterImage);

        await ichigoClient.SendEmbedMessageAsync(
            Context.Message.ChannelId,
            new MessageProperties { Embeds = [embed], Attachments = [attachment] });
    }

    [Command("hgnext")]
    [UsedImplicitly]
    public async Task NextEvent()
    {
        if (!IsOwner())
        {
            return;
        }

        EventResult result;
        try
        {
            result = gamesService.AdvanceEvent(Context.Message.ChannelId);
        }
        catch (InvalidOperationException ex)
        {
            await ichigoClient.SendMessageAsync(Context.Message.ChannelId, ex.Message);
            return;
        }

        await using var image = IconCompositor.Compose(result.Participants);

        var title = result.Phase == Phase.Day ? $"Day {result.Day}" : $"Night {result.Day}";

        var description = new StringBuilder();
        if (result.PhaseStarted)
        {
            var header = result.Phase == Phase.Day
                ? $"☀️ Day {result.Day} begins."
                : $"🌙 Night {result.Day} falls.";
            description.AppendLine($"**{header}**");
            description.AppendLine();
        }
        description.AppendLine(result.Line);

        var fallen = result.Participants.Where(p => p.Died).Select(p => p.Name).ToList();
        if (fallen.Count > 0)
        {
            description.AppendLine();
            description.AppendLine($"💀 Fallen: {string.Join(", ", fallen.Select(n => $"~~{n}~~"))}");
        }

        var eventEmbed = EmbedHelper.Build(title, description.ToString());
        eventEmbed.Image = new EmbedImageProperties("attachment://event.png");
        eventEmbed.Footer = new EmbedFooterProperties { Text = $"Survivors: {result.Survivors.Count}" };

        var attachment = new AttachmentProperties("event.png", image);
        await ichigoClient.SendEmbedMessageAsync(
            Context.Message.ChannelId,
            new MessageProperties { Embeds = [eventEmbed], Attachments = [attachment] });

        if (!result.IsOver)
        {
            return;
        }

        var template = HungerGameEvents.WinnerLines[Random.Shared.Next(HungerGameEvents.WinnerLines.Length)];
        var ending = template.Replace("{0}", result.Winner!);

        var endEmbed = EmbedHelper.Build("The Games Conclude", ending);
        endEmbed.Image = new EmbedImageProperties("attachment://winner.png");

        await using var winnerImage = IconCompositor.Compose(
            [new EventParticipant(result.Winner!, result.WinnerIconPath!, false)]);
        var winnerAttachment = new AttachmentProperties("winner.png", winnerImage);

        await ichigoClient.SendEmbedMessageAsync(
            Context.Message.ChannelId,
            new MessageProperties { Embeds = [endEmbed], Attachments = [winnerAttachment] });
    }

    [Command("hgstatus")]
    [UsedImplicitly]
    public async Task Status()
    {
        if (!IsOwner())
        {
            return;
        }

        var game = gamesService.GetGame(Context.Message.ChannelId);
        if (game is null)
        {
            await ichigoClient.SendMessageAsync(Context.Message.ChannelId, "No active Hunger Games in this channel.");
            return;
        }

        var alive = game.Tributes.Where(t => t.Alive).Select(t => $"• **{t.Name}**").ToList();
        var dead = game.Tributes.Where(t => !t.Alive).Select(t => $"• ~~{t.Name}~~").ToList();

        var description = new StringBuilder();
        description.AppendLine($"**Alive ({alive.Count})**");
        description.AppendLine(alive.Count > 0 ? string.Join("\n", alive) : "— none —");
        description.AppendLine();
        description.AppendLine($"**Fallen ({dead.Count})**");
        description.AppendLine(dead.Count > 0 ? string.Join("\n", dead) : "— none —");

        var embed = EmbedHelper.Build($"Hunger Games — Day {game.Day}", description.ToString());
        await ichigoClient.SendEmbedMessageAsync(
            Context.Message.ChannelId,
            new MessageProperties { Embeds = [embed] });
    }

    [Command("hgend")]
    [UsedImplicitly]
    public async Task EndGame()
    {
        if (!IsOwner())
        {
            return;
        }

        var removed = gamesService.EndGame(Context.Message.ChannelId);
        await ichigoClient.SendMessageAsync(
            Context.Message.ChannelId,
            removed ? "The Games have been called off." : "No active Hunger Games in this channel.");
    }

    private bool IsOwner()
    {
        var ownerUserId = configuration.GetValue<ulong>("Discord:OwnerUserId");
        return Context.Message.Author.Id == ownerUserId;
    }
}
