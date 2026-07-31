using System.Text;
using IchigoHoshimiya.Context;
using IchigoHoshimiya.Helpers;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace IchigoHoshimiya.Modules.InteractionModules;

[UsedImplicitly]
public class WwmExportButtonModule(WwmDbContext context, ILogger<WwmExportButtonModule> logger)
    : ComponentInteractionModule<ButtonInteractionContext>
{
    private const int ExportWindowDays = 7;

    [ComponentInteraction("wwm-export")]
    [UsedImplicitly]
    public async Task Export(long numberId)
    {
        await Context.Interaction.SendResponseAsync(
            InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        var player = await context.Players
                                  .AsNoTracking()
                                  .FirstOrDefaultAsync(p => p.NumberId == numberId);

        if (player is null)
        {
            await Respond($"`{numberId}` is not on file.");

            return;
        }

        var since = DateTime.UtcNow.AddDays(-ExportWindowDays);

        var snapshots = await context.Snapshots
                                     .Where(s => s.NumberId == numberId && s.FetchedAtUtc >= since)
                                     .OrderByDescending(s => s.FetchedAtUtc)
                                     .Include(s => s.Xinfa)
                                     .Include(s => s.Skills)
                                     .Include(s => s.Gear)
                                     .ThenInclude(g => g.Affixes)
                                     .Include(s => s.Gear)
                                     .ThenInclude(g => g.BaseAttrs)
                                     .AsSplitQuery()
                                     .AsNoTracking()
                                     .ToListAsync();

        if (snapshots.Count == 0)
        {
            await Respond($"No builds recorded for **{player.Name}** in the last {ExportWindowDays} days.");

            return;
        }

        var markdown = WwmBuildMarkdownWriter.Write(player, snapshots, ExportWindowDays);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

        var fileName = $"{Slug(player.Name)}-{numberId}-{ExportWindowDays}d.md";

        logger.LogInformation(
            "Exported {Count} WWM build(s) for {NumberId} as {Bytes} bytes of markdown",
            snapshots.Count,
            numberId,
            stream.Length);

        await Context.Interaction.ModifyResponseAsync(message =>
        {
            message.Content = $"📄 {snapshots.Count} build(s) recorded for **{player.Name}** " +
                              $"in the last {ExportWindowDays} days.";

            message.Attachments = [new AttachmentProperties(fileName, stream)];
        });
    }

    private Task Respond(string content) =>
        Context.Interaction.ModifyResponseAsync(message => message.WithContent(content));

    private static string Slug(string value)
    {
        var slug = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                slug.Append(char.ToLowerInvariant(character));
            }
            else if (slug.Length > 0 && slug[^1] != '-')
            {
                slug.Append('-');
            }
        }

        return slug.Length == 0 ? "player" : slug.ToString().Trim('-');
    }
}
