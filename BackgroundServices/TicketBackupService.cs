using System.Text;
using IchigoHoshimiya.Context;
using IchigoHoshimiya.Entities.General;
using IchigoHoshimiya.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IchigoHoshimiya.BackgroundServices;

public class TicketBackupService(
    ILogger<TicketBackupService> logger,
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly RateLimiter _rateLimiter = new();

    // Run frequently to minimize data loss before deletion
    private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Ticket Backup Service started.");

        // On first run, dump all existing tickets to files if configured
        var dumpExistingOnStartup = configuration.GetValue("DumpExistingTicketsOnStartup", false);

        if (dumpExistingOnStartup)
        {
            try
            {
                await DumpExistingTicketsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during initial ticket dump");
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAndArchiveTicketsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error running TicketBackupService");
            }

            await Task.Delay(_syncInterval, stoppingToken);
        }
    }

    private async Task SyncAndArchiveTicketsAsync(CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IchigoContext>();
        var client = scope.ServiceProvider.GetRequiredService<IClient>();

        var guildId = ulong.Parse(configuration["GuildId"]!);

        var ticketCategoryIds = configuration.GetSection("TicketCategoryIds")
                                             .Get<string[]>()
                                            ?.Select(ulong.Parse)
                                             .ToArray() ??
                                [];

        if (ticketCategoryIds.Length == 0)
        {
            logger.LogWarning("No ticket categories configured. Service will not process any tickets.");

            return;
        }

        var backupChannelId = ulong.Parse(configuration["BackupLogChannelId"]!);

        var delayBetweenChannels = configuration.GetValue("RateLimitDelayBetweenChannelsMs", 500);
        var delayBetweenFileUploads = configuration.GetValue("RateLimitDelayBetweenFileUploadsMs", 1000);

        await _rateLimiter.WaitAsync("discord_api", 100);
        var discordChannels = await client.GetChannelsInCategoriesAsync(guildId, ticketCategoryIds);
        var activeChannelIds = discordChannels.Select(c => c.Id).ToHashSet();

        logger.LogInformation($"Syncing {discordChannels.Count()} active ticket channels...");

        var channelCount = 0;

        foreach (var channel in discordChannels)
        {
            channelCount++;

            if (channelCount > 1)
            {
                await Task.Delay(delayBetweenChannels, token);
            }

            try
            {
                var ticketDb = await dbContext.TrackedTickets
                                              .Include(t => t.Messages)
                                              .FirstOrDefaultAsync(t => t.ChannelId == channel.Id, token);

                if (ticketDb == null)
                {
                    ticketDb = new TrackedTicket
                    {
                        ChannelId = channel.Id,
                        TicketName = channel.Name
                    };

                    dbContext.TrackedTickets.Add(ticketDb);
                }

                var lastMessageId = ticketDb.Messages.Any()
                    ? ticketDb.Messages.Max(m => m.DiscordMessageId)
                    : 0;

                await _rateLimiter.WaitAsync("discord_message_fetch", 300);
                var newMessages = await client.GetMessagesAfterIdAsync(channel.Id, lastMessageId);

                if (newMessages.Any())
                {
                    logger.LogDebug($"Found {newMessages.Count()} new messages in {channel.Name}");

                    foreach (var msg in newMessages)
                    {
                        ticketDb.Messages.Add(
                            new TicketMessage
                            {
                                DiscordMessageId = msg.Id,
                                ChannelId = channel.Id,
                                AuthorId = msg.Author.Id,
                                AuthorName = msg.Author.Username,
                                Content = msg.Content,
                                Timestamp = msg.CreatedAt,
                                AttachmentUrls = msg.Attachments.Select(a => a.Url).ToList()
                            });
                    }

                    ticketDb.LastSyncedAt = DateTimeOffset.UtcNow;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error syncing channel {channel.Name}");
            }
        }

        await dbContext.SaveChangesAsync(token);
        logger.LogInformation("Sync completed.");

        var closedTickets = await dbContext.TrackedTickets
                                           .Where(t => !t.IsClosed && !activeChannelIds.Contains(t.ChannelId))
                                           .Include(t => t.Messages)
                                           .ToListAsync(token);

        if (closedTickets.Count != 0)
        {
            logger.LogInformation($"Found {closedTickets.Count} closed ticket(s) to archive...");
        }

        var uploadCount = 0;

        foreach (var closedTicket in closedTickets)
        {
            uploadCount++;

            if (uploadCount > 1)
            {
                await Task.Delay(delayBetweenFileUploads, token);
            }

            try
            {
                logger.LogInformation($"Archiving ticket {closedTicket.TicketName}...");

                var transcript = GenerateTranscript(closedTicket);
                var fileName = $"ticket-log-{closedTicket.TicketName}-{DateTime.UtcNow:yyyyMMdd}.txt";

                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(transcript));

                await _rateLimiter.WaitAsync("discord_file_upload", 1000);

                await client.SendFileAsync(
                    backupChannelId,
                    stream,
                    fileName,
                    $"🔒 **Ticket Closed Backup**: {closedTicket.TicketName} (ID: {closedTicket.ChannelId})"
                );

                closedTicket.IsClosed = true;

                logger.LogInformation($"Successfully archived {closedTicket.TicketName}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error archiving ticket {closedTicket.TicketName}");
            }
        }

        await dbContext.SaveChangesAsync(token);
    }

    private async Task DumpExistingTicketsAsync(CancellationToken token)
    {
        logger.LogInformation("Starting initial dump of all existing tickets...");

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IchigoContext>();
        var client = scope.ServiceProvider.GetRequiredService<IClient>();

        var guildId = ulong.Parse(configuration["GuildId"]!);

        var ticketCategoryIds = configuration.GetSection("TicketCategoryIds")
                                             .Get<string[]>()
                                            ?.Select(ulong.Parse)
                                             .ToArray() ??
                                [];

        if (ticketCategoryIds.Length == 0)
        {
            logger.LogWarning("No ticket categories configured. Skipping initial dump.");

            return;
        }

        var backupChannelId = ulong.Parse(configuration["BackupLogChannelId"]!);
        var delayBetweenChannels = configuration.GetValue("RateLimitDelayBetweenChannelsDumpMs", 1000);
        var delayBetweenFileUploads = configuration.GetValue("RateLimitDelayBetweenFileUploadsDumpMs", 2000);

        await _rateLimiter.WaitAsync("discord_api", 100);
        var discordChannels = (await client.GetChannelsInCategoriesAsync(guildId, ticketCategoryIds)).ToList();

        logger.LogInformation($"Found {discordChannels.Count} existing ticket(s) to dump.");

        var processedCount = 0;

        foreach (var channel in discordChannels)
        {
            processedCount++;

            if (processedCount > 1)
            {
                await Task.Delay(delayBetweenChannels, token);
            }

            try
            {
                var existingTicket = await dbContext.TrackedTickets
                                                    .FirstOrDefaultAsync(t => t.ChannelId == channel.Id, token);

                if (existingTicket != null)
                {
                    logger.LogInformation(
                        $"[{processedCount}/{discordChannels.Count}] ⏩ Skipping {channel.Name} (Already exists in DB)");

                    continue;
                }

                logger.LogInformation($"[{processedCount}/{discordChannels.Count}] 📥 Dumping ticket: {channel.Name}");

                var ticketDb = new TrackedTicket
                {
                    ChannelId = channel.Id,
                    TicketName = channel.Name,
                    LastSyncedAt = DateTimeOffset.UtcNow
                };

                await _rateLimiter.WaitAsync("discord_message_fetch", 500);
                var allMessages = await client.GetMessagesAfterIdAsync(channel.Id, 0);

                if (allMessages.Any())
                {
                    foreach (var msg in allMessages)
                    {
                        ticketDb.Messages.Add(
                            new TicketMessage
                            {
                                DiscordMessageId = msg.Id,
                                ChannelId = channel.Id,
                                AuthorId = msg.Author.Id,
                                AuthorName = msg.Author.Username,
                                Content = msg.Content,
                                Timestamp = msg.CreatedAt,
                                AttachmentUrls = msg.Attachments.Select(a => a.Url).ToList()
                            });
                    }
                }
                
                dbContext.TrackedTickets.Add(ticketDb);
                await dbContext.SaveChangesAsync(token);
                
                var transcript = GenerateTranscript(ticketDb);
                var fileName = $"ticket-initial-{ticketDb.TicketName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt";

                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(transcript));
                await _rateLimiter.WaitAsync("discord_file_upload", 1000);
                await Task.Delay(delayBetweenFileUploads, token);

                await client.SendFileAsync(
                    backupChannelId,
                    stream,
                    fileName,
                    $"📋 **Initial Backup**: {ticketDb.TicketName} (ID: {ticketDb.ChannelId}) - {ticketDb.Messages.Count} messages"
                );

                logger.LogInformation(
                    $"[{processedCount}/{discordChannels.Count}] ✓ Successfully dumped {channel.Name}");
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    $"[{processedCount}/{discordChannels.Count}] ✗ Failed to dump ticket: {channel.Name}");
                
                dbContext.ChangeTracker.Clear();
            }
        }

        logger.LogInformation("Initial ticket dump completed!");
    }

    private string GenerateTranscript(TrackedTicket ticket)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"--- Transcript for {ticket.TicketName} ---");
        sb.AppendLine($"Channel ID: {ticket.ChannelId}");
        sb.AppendLine($"Exported At: {DateTime.UtcNow:F} UTC");
        sb.AppendLine(new string('-', 30));
        sb.AppendLine();

        foreach (var msg in ticket.Messages.OrderBy(m => m.Timestamp))
        {
            sb.AppendLine($"[{msg.Timestamp:yyyy-MM-dd HH:mm:ss}] {msg.AuthorName}: {msg.Content}");

            if (msg.AttachmentUrls.Count > 0)
            {
                sb.AppendLine("  [Attachments]:");

                foreach (var url in msg.AttachmentUrls)
                {
                    sb.AppendLine($"  - {url}");
                }
            }
        }

        return sb.ToString();
    }
}