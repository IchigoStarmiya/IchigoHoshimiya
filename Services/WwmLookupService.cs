using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IchigoHoshimiya.Context;
using IchigoHoshimiya.DTO;
using IchigoHoshimiya.Entities.Wwm;
using IchigoHoshimiya.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IchigoHoshimiya.Services;

public class WwmSettings
{
    public string BaseUrl { get; set; } = "https://wwm.ratz-gg.net";

    public string SessionCookie { get; set; } = string.Empty;

    // How often to re-poll a queued job's status. Kept slow on purpose: a scan takes seconds to
    // tens of seconds, so tight polling only adds requests without returning the result sooner.
    public int PollIntervalMs { get; set; } = 5000;

    public int TimeoutSeconds { get; set; } = 180;

    // How often the tracker sweeps the whole watchlist.
    public int CheckIntervalMinutes { get; set; } = 5;

    // Gap between players inside a sweep. Every tracked lookup is a forced scan, so this is the
    // main lever for staying under upstream's rate limits.
    public int RequestDelaySeconds { get; set; } = 15;

    public ulong AlertChannelId { get; set; }
}

public class WwmLookupException(string message) : Exception(message);

public class WwmAuthenticationException(string message) : WwmLookupException(message);

// The player claimed their account, making the profile private. Permanent until they unclaim it —
// retrying burns a scan slot for nothing.
public class WwmProfileClaimedException(string message) : WwmLookupException(message);

public class WwmLookupService(
    HttpClient httpClient,
    WwmDbContext context,
    IOptionsMonitor<WwmSettings> options,
    ILogger<WwmLookupService> logger) : IWwmLookupService
{
    private static readonly string[] SFailedStatuses = ["error", "failed", "not_found", "cancelled"];

    // Read per call rather than cached, so a rotated session cookie in appsettings.json takes effect
    // without a restart.
    private WwmSettings Settings => options.CurrentValue;

    public async Task<WwmLookupOutcome> LookupAndStore(
        WwmLookupType type,
        string query,
        bool forceFresh = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query must not be empty.", nameof(query));
        }

        var job = await SubmitLookup(type, query, forceFresh, cancellationToken);

        if (job.Result is null)
        {
            logger.LogInformation("WWM lookup {JobId} queued for {Type} '{Query}'", job.Id, type, query);

            job = await PollUntilComplete(job.Id, cancellationToken);
        }
        else if (job.Stale == true && !string.IsNullOrEmpty(job.RefreshId))
        {
            job = await FollowRefresh(job, cancellationToken);
        }

        if (job.Result is null)
        {
            throw new WwmLookupException($"WWM lookup for '{query}' completed without a result.");
        }

        return await Store(job, type, query, cancellationToken);
    }

    // A stale cache hit comes back with the old payload plus a refresh job already running upstream.
    // Prefer the refreshed data, but never fail the lookup over it — the stale payload is still usable.
    private async Task<WwmLookupJobDto> FollowRefresh(WwmLookupJobDto cached, CancellationToken cancellationToken)
    {
        try
        {
            var refreshed = await PollUntilComplete(cached.RefreshId!, cancellationToken);

            return refreshed.Result is null ? cached : refreshed;
        }
        catch (Exception exception) when (exception is WwmLookupException or HttpRequestException)
        {
            logger.LogWarning(
                exception,
                "WWM refresh {RefreshId} failed; falling back to the stale cached result",
                cached.RefreshId);

            return cached;
        }
    }

    private async Task<WwmLookupJobDto> SubmitLookup(
        WwmLookupType type,
        string query,
        bool forceFresh,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(
            HttpMethod.Post,
            forceFresh ? "/wwm-api/lookup?fresh=1" : "/wwm-api/lookup");

        request.Content = JsonContent.Create(new { type = ToWireType(type), query });

        var job = await Send(request, cancellationToken);

        // The POST either queues a job (id, status "pending") or answers straight from cache with the
        // full result and no id at all.
        if (job.Result is null && string.IsNullOrEmpty(job.Id))
        {
            throw new WwmLookupException($"WWM lookup for '{query}' was not accepted (status '{job.Status}').");
        }

        return job;
    }

    private async Task<WwmLookupJobDto> PollUntilComplete(string jobId, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(Settings.TimeoutSeconds);
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            using var request = CreateRequest(HttpMethod.Get, $"/wwm-api/lookup/{jobId}");

            var job = await Send(request, cancellationToken);

            if (string.Equals(job.Status, "done", StringComparison.OrdinalIgnoreCase))
            {
                return job;
            }

            if (SFailedStatuses.Contains(job.Status, StringComparer.OrdinalIgnoreCase))
            {
                throw new WwmLookupException(
                    $"WWM lookup {jobId} failed with status '{job.Status}' (code {job.ErrorCode?.ToString() ?? "none"}).");
            }

            if (stopwatch.Elapsed > timeout)
            {
                throw new WwmLookupException(
                    $"WWM lookup {jobId} did not complete within {Settings.TimeoutSeconds}s (last status '{job.Status}').");
            }

            await Task.Delay(Settings.PollIntervalMs, cancellationToken);
        }
    }

    private async Task<WwmLookupJobDto> Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new WwmLookupException(
                $"WWM API returned {(int)response.StatusCode} for {request.Method} {request.RequestUri}.");
        }

        var job = await response.Content.ReadFromJsonAsync<WwmLookupJobDto>(cancellationToken)
                  ?? throw new WwmLookupException($"WWM API returned an empty body for {request.RequestUri}.");

        if (job.NeedsLogin == true || string.Equals(job.Error, "login_required", StringComparison.OrdinalIgnoreCase))
        {
            throw new WwmAuthenticationException(
                "The WWM session cookie is missing or expired — refresh 'Wwm:SessionCookie' in appsettings.json. " +
                "Results already cached upstream stay readable without it; new scans do not.");
        }

        if (string.Equals(job.Status, "claimed", StringComparison.OrdinalIgnoreCase))
        {
            throw new WwmProfileClaimedException(
                "This player has claimed their account — their profile is private and cannot be scanned.");
        }

        if (!string.IsNullOrEmpty(job.Error))
        {
            throw new WwmLookupException($"WWM API error: {job.Error}");
        }

        return job;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, new Uri(new Uri(Settings.BaseUrl), path));

        request.Headers.Referrer = new Uri(new Uri(Settings.BaseUrl), "/playerLookup");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(Settings.SessionCookie))
        {
            request.Headers.Add("Cookie", $"rg_sess={Settings.SessionCookie}");
        }

        if (method == HttpMethod.Post)
        {
            request.Headers.Add("Origin", Settings.BaseUrl.TrimEnd('/'));
        }

        return request;
    }

    private async Task<WwmLookupOutcome> Store(
        WwmLookupJobDto job,
        WwmLookupType type,
        string query,
        CancellationToken cancellationToken)
    {
        var snapshot = WwmLookupMapper.ToSnapshot(job, type, query);

        snapshot.BuildHash = WwmBuildFingerprint.Compute(snapshot);

        var player = await context.Players
                                  .FirstOrDefaultAsync(p => p.NumberId == snapshot.NumberId, cancellationToken);

        if (player is null)
        {
            player = new WwmPlayer
            {
                NumberId = snapshot.NumberId,
                FirstSeenAtUtc = snapshot.FetchedAtUtc
            };

            context.Players.Add(player);
        }

        WwmLookupMapper.ApplyToPlayer(player, snapshot);

        var latest = await context.Snapshots
                                  .Where(s => s.NumberId == snapshot.NumberId)
                                  .OrderByDescending(s => s.FetchedAtUtc)
                                  .FirstOrDefaultAsync(cancellationToken);

        // Only distinct builds earn a row. An unchanged loadout still updates the player's
        // last-seen stamp so we know the check happened.
        if (latest is not null && latest.BuildHash == snapshot.BuildHash)
        {
            await context.SaveChangesAsync(cancellationToken);

            logger.LogDebug(
                "WWM build for {Name} ({NumberId}) is unchanged since snapshot {SnapshotId}",
                snapshot.Name,
                snapshot.NumberId,
                latest.Id);

            return new WwmLookupOutcome(latest, false);
        }

        context.Snapshots.Add(snapshot);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Stored new WWM build {SnapshotId} for {Name} ({NumberId}): {GearCount} gear pieces, hash {BuildHash}",
            snapshot.Id,
            snapshot.Name,
            snapshot.NumberId,
            snapshot.Gear.Count,
            snapshot.BuildHash[..12]);

        return new WwmLookupOutcome(snapshot, true);
    }

    private static string ToWireType(WwmLookupType type) => type switch
    {
        WwmLookupType.NumberId => "number_id",
        WwmLookupType.Name => "name",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported WWM lookup type.")
    };
}
