using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IchigoHoshimiya.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace IchigoHoshimiya.BackgroundServices;

file record GraphQlResponse([property: JsonPropertyName("data")] DataPayload Data);

// You just gotta live with these warnings
file record DataPayload(
    [property: JsonPropertyName("dumpPagination")]
    DumpPaginator Paginator);

file record DumpPaginator([property: JsonPropertyName("data")] List<DumpEntry> Data);

file record DumpEntry([property: JsonPropertyName("link")] string Link);

public class AnimeThemesUpdaterSettings
{
    public string GraphQlEndpoint { get; init; } = string.Empty;
}

public class AnimeThemesDbUpdateService(
    ILogger<AnimeThemesDbUpdateService> logger,
    IOptions<AnimeThemesUpdaterSettings> settings,
    IConfiguration configuration,
    IAnimethemeCache animethemeCache)
    : BackgroundService
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    private readonly AnimeThemesUpdaterSettings _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AnimeThemes Update Service is starting.");

        try
        {
            await animethemeCache.RefreshAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to warm animetheme cache at startup.");
        }

#if RELEASE
        await RunUpdateAsync(stoppingToken);
#endif
        while (!stoppingToken.IsCancellationRequested)
        {
            // Calculate delay until the next 00:00 UTC
            DateTime nowUtc = DateTime.UtcNow;
            DateTime nextRunTime = nowUtc.Date.AddDays(1); // Midnight tomorrow
            TimeSpan delay = nextRunTime - nowUtc;

            logger.LogInformation("Next database update scheduled for: {NextRunTime} UTC.", nextRunTime);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break; // Service is stopping
            }

            await RunUpdateAsync(stoppingToken);
        }
    }

    private async Task RunUpdateAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Starting database update...");

            var dumpUrl = await RetryAsync(
                () => GetDumpUrlAsync(stoppingToken),
                stoppingToken);

            if (string.IsNullOrEmpty(dumpUrl))
            {
                logger.LogWarning("Could not retrieve SQL dump URL. Skipping update.");

                return;
            }

            await RetryAsync(
                async () =>
                {
                    await ExecuteSqlScriptFromUrlAsync(dumpUrl, stoppingToken);

                    return true;
                },
                stoppingToken);

            logger.LogInformation("Database update completed successfully.");

            await animethemeCache.RefreshAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during the database update.");
        }
    }

    private async Task<T> RetryAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;

        for (var attempt = 1;; attempt++)
        {
            try
            {
                return await action();
            }
            catch (HttpRequestException ex) when (attempt < maxAttempts)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));

                logger.LogWarning(
                    ex,
                    "AnimeThemes request failed on attempt {Attempt}/{Max}. Retrying in {Delay}s.",
                    attempt,
                    maxAttempts,
                    delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private async Task<string?> GetDumpUrlAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Querying GraphQL endpoint for dump URL...");

        var queryText = await File.ReadAllTextAsync("Queries/Animethemes/DumpQuery.graphql", cancellationToken);
        var body = JsonSerializer.Serialize(new { query = queryText });

        // Shell out to curl: .NET's TLS/HTTP-2 fingerprint gets 403'd by Cloudflare on this
        // endpoint, while curl from the same machine passes. Cheaper than a curl-impersonate port.
        var psi = new ProcessStartInfo
        {
            FileName = "curl",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        psi.ArgumentList.Add("-sS");
        psi.ArgumentList.Add("--fail-with-body");
        psi.ArgumentList.Add("--max-time");
        psi.ArgumentList.Add("30");
        psi.ArgumentList.Add("-X");
        psi.ArgumentList.Add("POST");
        psi.ArgumentList.Add("-H");
        psi.ArgumentList.Add("Content-Type: application/json");
        psi.ArgumentList.Add("-H");
        psi.ArgumentList.Add("Accept: application/json");
        psi.ArgumentList.Add("--data-binary");
        psi.ArgumentList.Add("@-");
        psi.ArgumentList.Add(_settings.GraphQlEndpoint);

        using var process = Process.Start(psi)
                            ?? throw new InvalidOperationException("Failed to start curl. Is it on PATH?");

        await process.StandardInput.WriteAsync(body.AsMemory(), cancellationToken);
        process.StandardInput.Close();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            throw new HttpRequestException(
                $"curl exited with code {process.ExitCode}: {stderr.Trim()} | body: {stdout.Trim()}");
        }

        var gqlResponse = JsonSerializer.Deserialize<GraphQlResponse>(stdout);
        var link = gqlResponse?.Data.Paginator.Data.FirstOrDefault()?.Link;

        logger.LogInformation("Found dump URL: {Link}", link);

        return link;
    }

    private async Task ExecuteSqlScriptFromUrlAsync(string url, CancellationToken cancellationToken)
    {
        logger.LogInformation("Applying SQL dump directly from URL: {Url}", url);

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var psi = new ProcessStartInfo
        {
            FileName = "curl",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        psi.ArgumentList.Add("-sS");
        psi.ArgumentList.Add("--fail");
        psi.ArgumentList.Add("--location");
        psi.ArgumentList.Add(url);

        using var process = Process.Start(psi)
                            ?? throw new InvalidOperationException("Failed to start curl. Is it on PATH?");

        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        using (var reader = new StreamReader(process.StandardOutput.BaseStream))
        {
            var scriptBuilder = new StringBuilder();

            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("--"))
                {
                    continue;
                }

                scriptBuilder.AppendLine(line);

                if (!line.TrimEnd().EndsWith(';'))
                {
                    continue;
                }

                var sqlStatement = scriptBuilder.ToString();
                scriptBuilder.Clear();

                await using var cmd = new MySqlCommand(sqlStatement, connection);
                cmd.CommandTimeout = 0;
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var stderr = await stderrTask;

            throw new HttpRequestException(
                $"curl exited with code {process.ExitCode} downloading dump: {stderr.Trim()}");
        }
    }
}