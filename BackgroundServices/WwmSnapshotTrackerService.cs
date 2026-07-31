using System.Globalization;
using IchigoHoshimiya.Context;
using IchigoHoshimiya.Entities.Wwm;
using IchigoHoshimiya.Interfaces;
using IchigoHoshimiya.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IchigoHoshimiya.BackgroundServices;

// Walks the tracked-player watchlist on an interval and records a snapshot whenever someone's build
// changes. Upstream requires a live session cookie for fresh scans, so an expired cookie pauses the
// cycle and pings the alert channel instead of quietly failing every run.
public class WwmSnapshotTrackerService(
    IServiceProvider serviceProvider,
    IOptionsMonitor<WwmSettings> options,
    IClient client,
    ILogger<WwmSnapshotTrackerService> logger)
    : BackgroundService
{
    private bool _sessionAlertSent;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutes = Math.Max(1, options.CurrentValue.CheckIntervalMinutes);
        var interval = TimeSpan.FromMinutes(minutes);

        using var timer = new PeriodicTimer(interval);

        logger.LogInformation(
            "WWM snapshot tracker started; forcing fresh scans for tracked players every {Minutes}m",
            minutes);

        try
        {
            do
            {
                var startedAt = DateTime.UtcNow;

                try
                {
                    await RunCycle(stoppingToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(exception, "WWM tracking cycle failed");
                }

                // Forced scans are queued upstream, so a sweep can outlast its own interval. The
                // timer simply fires again immediately; surface it so the real cadence is visible.
                var elapsed = DateTime.UtcNow - startedAt;

                if (elapsed > interval)
                {
                    logger.LogWarning(
                        "WWM sweep took {Elapsed:g}, longer than the {Interval:g} interval — players are being checked slower than configured",
                        elapsed,
                        interval);
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task RunCycle(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<WwmDbContext>();
        var lookupService = scope.ServiceProvider.GetRequiredService<IWwmLookupService>();

        var tracked = await context.TrackedPlayers
                                   .Where(p => p.Enabled)
                                   .OrderBy(p => p.LastCheckedAtUtc)
                                   .ToListAsync(stoppingToken);

        if (tracked.Count == 0)
        {
            return;
        }

        logger.LogInformation("Checking {Count} tracked WWM player(s)", tracked.Count);

        var delay = TimeSpan.FromSeconds(Math.Max(0, options.CurrentValue.RequestDelaySeconds));

        for (var i = 0; i < tracked.Count; i++)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var player = tracked[i];

            try
            {
                await CheckPlayer(lookupService, player, stoppingToken);
            }
            catch (WwmAuthenticationException exception)
            {
                player.LastCheckedAtUtc = DateTime.UtcNow;
                player.LastError = Truncate(exception.Message, 500);

                await context.SaveChangesAsync(stoppingToken);

                // Every remaining player would hit the same wall — stop and ask for a new cookie.
                await NotifySessionExpired(tracked.Count - i);

                return;
            }
            catch (WwmProfileClaimedException exception)
            {
                // Private profiles never resolve; stop spending scan slots on them.
                logger.LogInformation(
                    "WWM player {NumberId} has a claimed profile — disabling tracking",
                    player.NumberId);

                player.Enabled = false;
                player.LastCheckedAtUtc = DateTime.UtcNow;
                player.LastError = Truncate(exception.Message, 500);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "WWM lookup failed for {NumberId}", player.NumberId);

                player.LastCheckedAtUtc = DateTime.UtcNow;
                player.LastError = Truncate($"{exception.GetType().Name}: {exception.Message}", 500);
            }

            if (delay > TimeSpan.Zero && i < tracked.Count - 1)
            {
                await Task.Delay(delay, stoppingToken);
            }
        }

        await context.SaveChangesAsync(stoppingToken);
    }

    private async Task CheckPlayer(
        IWwmLookupService lookupService,
        WwmTrackedPlayer player,
        CancellationToken stoppingToken)
    {
        // Always forced: a cached copy can be up to four hours old, which defeats the point of a
        // five-minute sweep.
        var outcome = await lookupService.LookupAndStore(
            WwmLookupType.NumberId,
            player.NumberId.ToString(CultureInfo.InvariantCulture),
            forceFresh: true,
            stoppingToken);

        var now = DateTime.UtcNow;

        player.LastCheckedAtUtc = now;
        player.LastSuccessAtUtc = now;
        player.LastError = null;
        player.Label = outcome.Snapshot.Name;

        // A working lookup means the cookie is live again, so the next expiry can alert afresh.
        _sessionAlertSent = false;

        if (!outcome.BuildChanged)
        {
            return;
        }

        player.LastBuildChangeAtUtc = now;

        logger.LogInformation(
            "New WWM build recorded for {Name} ({NumberId})",
            outcome.Snapshot.Name,
            player.NumberId);
    }

    private async Task NotifySessionExpired(int remaining)
    {
        var channelId = options.CurrentValue.AlertChannelId;

        if (_sessionAlertSent || channelId == 0)
        {
            logger.LogWarning("WWM session cookie expired; {Remaining} tracked player(s) skipped", remaining);

            return;
        }

        _sessionAlertSent = true;

        var content =
            "⚠️ **WWM session cookie expired.** " +
            $"Build tracking is paused — {remaining} player(s) were skipped this cycle.\n" +
            "Replace `Wwm:SessionCookie` in `appsettings.json`; it reloads automatically, no restart needed.";

        try
        {
            await client.SendMessageAsync(channelId, content);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed to post the WWM session expiry alert to {ChannelId}", channelId);
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length > maxLength ? value[..maxLength] : value;
}
