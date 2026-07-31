using System.Globalization;
using System.Text;
using IchigoHoshimiya.Context;
using IchigoHoshimiya.Entities.Wwm;
using IchigoHoshimiya.Helpers;
using IchigoHoshimiya.Interfaces;
using IchigoHoshimiya.Services;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace IchigoHoshimiya.Modules.SlashCommands;

public class WwmSlashCommandModule(
    WwmDbContext context,
    IWwmLookupService lookupService,
    IConfiguration configuration)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    private const int MaxSnapshotsShown = 5;

    [SlashCommand("wwmplayer", "Show a tracked Where Winds Meet player's recent builds")]
    [UsedImplicitly]
    public async Task Player(
        [SlashCommandParameter(Name = "query", Description = "The player's name or numeric UID")]
        string query)
    {
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());

        var matches = await ResolvePlayers(query);

        if (matches.Count == 0)
        {
            await Edit($"No tracked player matches **{Sanitize(query)}**. Ask the owner to `/wwmtrack` them first.");

            return;
        }

        if (matches.Count > 1)
        {
            var options = string.Join(
                "\n",
                matches.Select(p => $"• **{Sanitize(p.Name)}** — `{p.NumberId}`"));

            await Edit($"**{Sanitize(query)}** matches several players — try again with the UID:\n{options}");

            return;
        }

        var player = matches[0];

        var snapshots = await context.Snapshots
                                     .Where(s => s.NumberId == player.NumberId)
                                     .OrderByDescending(s => s.FetchedAtUtc)
                                     .Take(MaxSnapshotsShown)
                                     .Include(s => s.Xinfa)
                                     .Include(s => s.Skills)
                                     .Include(s => s.Gear)
                                     .ThenInclude(g => g.Affixes)
                                     .AsSplitQuery()
                                     .ToListAsync();

        if (snapshots.Count == 0)
        {
            await Edit($"**{Sanitize(player.Name)}** (`{player.NumberId}`) has no recorded builds yet.");

            return;
        }

        var totalBuilds = await context.Snapshots.CountAsync(s => s.NumberId == player.NumberId);

        var description = WwmProfileFormatter.BuildOverview(snapshots, out var rendered);

        var embed = EmbedHelper.Build($"{player.Name} · {player.NumberId}", description);

        var footer = new StringBuilder($"Showing {rendered} of {totalBuilds} recorded build");

        if (totalBuilds != 1)
        {
            footer.Append('s');
        }

        footer.Append(" · ").Append(player.Region ?? "unknown region").Append(" · server ").Append(player.Server);

        embed.Footer = new EmbedFooterProperties { Text = footer.ToString() };
        embed.Timestamp = new DateTimeOffset(player.LastSeenAtUtc, TimeSpan.Zero);

        ActionRowProperties components = new([
            new ButtonProperties(
                $"wwm-export:{player.NumberId}",
                "Download last 7 days",
                ButtonStyle.Secondary)
        ]);

        await Context.Interaction.ModifyResponseAsync(message =>
        {
            message.Embeds = [embed];
            message.Components = [components];
        });
    }

    [SlashCommand("wwmtrack", "Track a Where Winds Meet player and snapshot their build changes")]
    [UsedImplicitly]
    public async Task Track(
        [SlashCommandParameter(Name = "query", Description = "The player's name or numeric UID")]
        string query)
    {
        if (!await EnsureOwner())
        {
            return;
        }

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());

        query = query.Trim();

        var isNumeric = long.TryParse(query, NumberStyles.None, CultureInfo.InvariantCulture, out var numberId);
        var type = isNumeric ? WwmLookupType.NumberId : WwmLookupType.Name;

        // The lookup runs first: a name only resolves to a UID once upstream answers.
        WwmLookupOutcome outcome;

        try
        {
            outcome = await lookupService.LookupAndStore(type, query, forceFresh: true);
        }
        catch (WwmProfileClaimedException)
        {
            await Edit($"❌ **{Sanitize(query)}** has claimed their account — their profile is private and cannot be scanned.");

            return;
        }
        catch (WwmAuthenticationException)
        {
            await Edit(isNumeric
                           ? await TrackWithoutBaseline(numberId)
                           : "❌ The WWM session cookie is expired, so a name cannot be resolved to a UID. " +
                             "Replace `Wwm:SessionCookie` in `appsettings.json`, or track by UID instead.");

            return;
        }
        catch (Exception exception)
        {
            await Edit(isNumeric
                           ? await TrackWithoutBaseline(numberId)
                           : $"❌ Lookup failed for **{Sanitize(query)}**: {exception.GetType().Name}: {exception.Message}");

            return;
        }

        var snapshot = outcome.Snapshot;

        var tracked = await context.TrackedPlayers.FirstOrDefaultAsync(p => p.NumberId == snapshot.NumberId);

        if (tracked is null)
        {
            tracked = new WwmTrackedPlayer
            {
                NumberId = snapshot.NumberId,
                AddedById = Context.User.Id
            };

            context.TrackedPlayers.Add(tracked);
        }
        else
        {
            tracked.Enabled = true;
        }

        var now = DateTime.UtcNow;

        tracked.Label = snapshot.Name;
        tracked.LastCheckedAtUtc = now;
        tracked.LastSuccessAtUtc = now;
        tracked.LastError = null;

        if (outcome.BuildChanged)
        {
            tracked.LastBuildChangeAtUtc = snapshot.FetchedAtUtc;
        }

        await context.SaveChangesAsync();

        await Edit($"✅ Now tracking **{Sanitize(snapshot.Name)}** (`{snapshot.NumberId}`). " +
                   (outcome.BuildChanged
                       ? "Baseline build recorded."
                       : "Their current build was already on file."));
    }

    [SlashCommand("wwmuntrack", "Stop tracking a Where Winds Meet player")]
    [UsedImplicitly]
    public async Task Untrack(
        [SlashCommandParameter(Name = "query", Description = "The player's name or numeric UID")]
        string query)
    {
        if (!await EnsureOwner())
        {
            return;
        }

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());

        var matches = await ResolvePlayers(query);

        var numberIds = matches.Select(p => p.NumberId).ToList();

        // Fall back to the raw UID so an entry added before its first successful scan can still be removed.
        if (numberIds.Count == 0 &&
            long.TryParse(query.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
        {
            numberIds.Add(parsed);
        }

        var tracked = await context.TrackedPlayers
                                   .Where(p => numberIds.Contains(p.NumberId) && p.Enabled)
                                   .ToListAsync();

        if (tracked.Count == 0)
        {
            await Edit($"**{Sanitize(query)}** is not being tracked.");

            return;
        }

        if (tracked.Count > 1)
        {
            var options = string.Join(
                "\n",
                tracked.Select(p => $"• **{Sanitize(p.Label ?? "unknown")}** — `{p.NumberId}`"));

            await Edit($"**{Sanitize(query)}** matches several tracked players — try again with the UID:\n{options}");

            return;
        }

        // Keep the entry and its snapshot history; just stop checking it.
        tracked[0].Enabled = false;

        await context.SaveChangesAsync();

        await Edit($"🛑 Stopped tracking **{Sanitize(tracked[0].Label ?? query)}**. Existing builds are kept.");
    }

    [SlashCommand("wwmtracked", "List the tracked Where Winds Meet players")]
    [UsedImplicitly]
    public async Task Tracked()
    {
        if (!await EnsureOwner())
        {
            return;
        }

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());

        var tracked = await context.TrackedPlayers
                                   .Where(p => p.Enabled)
                                   .OrderBy(p => p.NumberId)
                                   .ToListAsync();

        if (tracked.Count == 0)
        {
            await Edit("No players are being tracked. Add one with `/wwmtrack`.");

            return;
        }

        var counts = await context.Snapshots
                                  .GroupBy(s => s.NumberId)
                                  .Select(g => new { NumberId = g.Key, Builds = g.Count() })
                                  .ToDictionaryAsync(g => g.NumberId, g => g.Builds);

        var builder = new StringBuilder($"**Tracked WWM players ({tracked.Count})**\n");

        foreach (var player in tracked)
        {
            counts.TryGetValue(player.NumberId, out var builds);

            builder.Append("• **")
                   .Append(Sanitize(player.Label ?? "unknown"))
                   .Append("** `")
                   .Append(player.NumberId)
                   .Append("` — ")
                   .Append(builds)
                   .Append(builds == 1 ? " build" : " builds")
                   .Append(", last checked ")
                   .Append(player.LastCheckedAtUtc is { } checkedAt
                               ? $"<t:{new DateTimeOffset(checkedAt, TimeSpan.Zero).ToUnixTimeSeconds()}:R>"
                               : "never");

            if (!string.IsNullOrEmpty(player.LastError))
            {
                builder.Append(" ⚠️");
            }

            builder.Append('\n');
        }

        await Edit(builder.ToString());
    }

    // Accepts a numeric UID or a name; name matching is exact first so an exact hit is never
    // buried among substring matches.
    private async Task<List<WwmPlayer>> ResolvePlayers(string query)
    {
        query = query.Trim();

        if (long.TryParse(query, NumberStyles.None, CultureInfo.InvariantCulture, out var numberId))
        {
            var byId = await context.Players
                                    .Where(p => p.NumberId == numberId)
                                    .ToListAsync();

            if (byId.Count > 0)
            {
                return byId;
            }
        }

        var exact = await context.Players
                                 .Where(p => p.Name == query)
                                 .ToListAsync();

        if (exact.Count > 0)
        {
            return exact;
        }

        return await context.Players
                            .Where(p => EF.Functions.Like(p.Name, $"%{query}%"))
                            .OrderBy(p => p.Name)
                            .Take(10)
                            .ToListAsync();
    }

    private async Task<string> TrackWithoutBaseline(long numberId)
    {
        var tracked = await context.TrackedPlayers.FirstOrDefaultAsync(p => p.NumberId == numberId);

        if (tracked is null)
        {
            context.TrackedPlayers.Add(new WwmTrackedPlayer
            {
                NumberId = numberId,
                AddedById = Context.User.Id
            });
        }
        else
        {
            tracked.Enabled = true;
        }

        await context.SaveChangesAsync();

        return $"✅ Now tracking `{numberId}`, but the baseline lookup failed. " +
               "The next tracker sweep will retry.";
    }

    private async Task<bool> EnsureOwner()
    {
        if (Context.User.Id == configuration.GetValue<ulong>("Discord:OwnerUserId"))
        {
            return true;
        }

        await Context.Interaction.SendResponseAsync(
            InteractionCallback.Message(new InteractionMessageProperties
            {
                Content = "You do not have permission to use this command.",
                Flags = MessageFlags.Ephemeral
            }));

        return false;
    }

    private Task Edit(string content) =>
        Context.Interaction.ModifyResponseAsync(message => message.WithContent(content));

    private static string Sanitize(string value) => value.Replace("`", "'").Replace("*", "\\*");
}
