using IchigoHoshimiya.Helpers;
using IchigoHoshimiya.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IchigoHoshimiya.BackgroundServices;

public class ActivitySignupPosterService(
    IServiceProvider serviceProvider,
    IActivitySignupSchedule schedule,
    IOptions<ActivitySignupSettings> options,
    ILogger<ActivitySignupPosterService> logger)
    : BackgroundService
{
    // Small buffer so the post/close mark has definitely passed when we wake up
    private static readonly TimeSpan WakeUpBuffer = TimeSpan.FromSeconds(5);

    // Test signups can be created while we sleep, so never sleep longer than this
    private static readonly TimeSpan MaxSleep = TimeSpan.FromMinutes(1);

    private readonly ActivitySignupSettings _settings = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var nextWakeUtc = DateTime.UtcNow + MaxSleep;

            try
            {
                using var scope = serviceProvider.CreateScope();
                var signupService = scope.ServiceProvider.GetRequiredService<IActivitySignupService>();
                var now = DateTime.UtcNow;

                await signupService.CloseDueSignupsAsync(now);

                if (_settings.ChannelId != 0)
                {
                    await signupService.PostDueSignupsAsync(_settings.ChannelId, now);

                    var nextPost = schedule.GetNextPostUtc(now);
                    nextWakeUtc = nextPost < nextWakeUtc ? nextPost : nextWakeUtc;
                }

                if (await signupService.GetNextCloseUtcAsync() is { } nextClose && nextClose < nextWakeUtc)
                {
                    nextWakeUtc = nextClose;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to post or close activity signups");
            }

            var delay = nextWakeUtc - DateTime.UtcNow + WakeUpBuffer;

            await Task.Delay(delay > TimeSpan.Zero ? delay : WakeUpBuffer, stoppingToken);
        }
    }
}
