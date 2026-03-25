using IchigoHoshimiya.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IchigoHoshimiya.BackgroundServices;

/// <summary>
/// Runs once per hour and closes any open scrim signups whose creation week's Friday has passed.
/// </summary>
public class ScrimAutoCloseService(
    IServiceProvider serviceProvider,
    ILogger<ScrimAutoCloseService> logger)
    : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run an initial check immediately on startup so signups that expired
        // while the bot was offline are closed right away.
        await CloseExpiredSignupsAsync(stoppingToken);

        using var timer = new PeriodicTimer(CheckInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CloseExpiredSignupsAsync(stoppingToken);
        }
    }

    private async Task CloseExpiredSignupsAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var scrimService = scope.ServiceProvider.GetRequiredService<IScrimService>();

            var expired = await scrimService.GetExpiredOpenSignupsAsync(stoppingToken);

            foreach (var signup in expired)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await scrimService.CloseSignupAsync(signup.Id, stoppingToken);
                    logger.LogInformation("Auto-closed scrim signup {SignupId} (message {MessageId})", signup.Id, signup.MessageId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to auto-close scrim signup {SignupId}", signup.Id);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down, nothing to do.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error in ScrimAutoCloseService");
        }
    }
}

