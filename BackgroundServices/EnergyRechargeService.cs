using System.Text;
using IchigoHoshimiya.Context;
using IchigoHoshimiya.Entities.General;
using IchigoHoshimiya.Helpers;
using IchigoHoshimiya.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IchigoHoshimiya.BackgroundServices;

public class EnergyRechargeService(
    IServiceProvider serviceProvider,
    IEnergyTrackerCalculator calculator,
    IOptions<EnergyTrackerSettings> options,
    IClient client,
    ILogger<EnergyRechargeService> logger)
    : BackgroundService
{
    // Small buffer so the recharge mark has definitely passed when we wake up
    private static readonly TimeSpan WakeUpBuffer = TimeSpan.FromSeconds(5);

    private readonly EnergyTrackerSettings _settings = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RechargeAllAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to recharge energy characters");
            }

            var delay = calculator.GetNextRechargeUtc(DateTime.UtcNow) - DateTime.UtcNow + WakeUpBuffer;

            await Task.Delay(delay > TimeSpan.Zero ? delay : WakeUpBuffer, stoppingToken);
        }
    }

    private async Task RechargeAllAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IchigoContext>();

        var characters = await db.EnergyCharacters.ToListAsync(stoppingToken);
        var now = DateTime.UtcNow;
        var alerts = new Dictionary<ulong, StringBuilder>();

        foreach (var character in characters)
        {
            calculator.ApplyRecharges(character, now);

            var energyAlert = !character.EnergyAlertSent
                              && calculator.GetEnergyLevel(character) == EnergyTrackerLevel.Critical;

            var ticketAlert = !character.TicketAlertSent
                              && calculator.GetNightmareTicketLevel(character) == EnergyTrackerLevel.Critical;

            calculator.AcknowledgeAlerts(character);

            if (!energyAlert && !ticketAlert)
            {
                continue;
            }

            if (!alerts.TryGetValue(character.UserId, out var message))
            {
                message = new StringBuilder($"<@{character.UserId}>");
                alerts[character.UserId] = message;
            }

            if (energyAlert)
            {
                message.Append('\n').Append(DescribeEnergyAlert(character, now));
            }

            if (ticketAlert)
            {
                message.Append('\n').Append(DescribeTicketAlert(character, now));
            }
        }

        await db.SaveChangesAsync(stoppingToken);

        if (_settings.AlertChannelId == 0)
        {
            return;
        }

        foreach (var message in alerts.Values)
        {
            try
            {
                await client.SendMessageAsync(_settings.AlertChannelId, message.ToString());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send energy alert to channel {ChannelId}", _settings.AlertChannelId);
            }
        }
    }

    private string DescribeEnergyAlert(EnergyCharacter character, DateTime now)
    {
        var fullAt = calculator.GetEnergyFullAtUtc(character, now);

        var when = fullAt is { } at
            ? $"full <t:{new DateTimeOffset(at).ToUnixTimeSeconds()}:R>"
            : "already full, recharges are being wasted";

        return $"⚡ **{character.Name}** is at {character.Energy}/{_settings.MaxEnergy} energy ({when}).";
    }

    private string DescribeTicketAlert(EnergyCharacter character, DateTime now)
    {
        var resetAt = new DateTimeOffset(calculator.GetNextNightmareResetUtc(now)).ToUnixTimeSeconds();

        return $"🎟️ **{character.Name}** has {character.NightmareTickets}/{_settings.MaxNightmareTickets} " +
               $"nightmare tickets, the reset <t:{resetAt}:R> will waste some.";
    }
}
