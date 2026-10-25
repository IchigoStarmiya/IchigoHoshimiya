using IchigoHoshimiya.Context;
using IchigoHoshimiya.Entities.General;
using IchigoHoshimiya.Helpers;
using IchigoHoshimiya.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord.Rest;

namespace IchigoHoshimiya.Services;

public class ActivitySignupService(
    IchigoContext context,
    IActivitySignupSchedule schedule,
    IActivitySignupMessageBuilder messageBuilder,
    IClient client,
    IOptions<ActivitySignupSettings> options,
    ILogger<ActivitySignupService> logger)
    : IActivitySignupService
{
    private readonly ulong _memberRoleId = options.Value.MemberRoleId;

    public async Task PostDueSignupsAsync(ulong channelId, DateTime nowUtc)
    {
        foreach (var slot in schedule.GetOpenSlots(nowUtc))
        {
            var alreadyPosted = await context.ActivitySignups
                                             .AnyAsync(s => !s.IsTest && s.SlotStartUtc == slot.StartUtc);

            if (alreadyPosted)
            {
                continue;
            }

            await PostAsync(new ActivitySignup
            {
                ChannelId = channelId,
                Title = messageBuilder.BuildTitle(slot),
                SlotStartUtc = slot.StartUtc,
                SlotEndUtc = slot.EndUtc,
                CloseAtUtc = slot.CloseAtUtc,
                IsSiegeDay = slot.IsSiegeDay
            });
        }
    }

    public Task<ActivitySignup> PostTestSignupAsync(ulong channelId, TimeSpan closesIn)
    {
        var now = DateTime.UtcNow;
        var slot = new ActivitySlot(now, now.AddHours(1), now, now + closesIn, false);

        return PostAsync(new ActivitySignup
        {
            ChannelId = channelId,
            Title = $"🧪 Test · {messageBuilder.BuildTitle(slot)}",
            SlotStartUtc = slot.StartUtc,
            SlotEndUtc = slot.EndUtc,
            CloseAtUtc = slot.CloseAtUtc,
            IsTest = true
        });
    }

    public async Task<ActivityChoiceResult> ToggleChoiceAsync(long signupId, ulong userId, ActivityChoice choice)
    {
        var signup = await context.ActivitySignups
                                  .Include(s => s.Entries)
                                  .FirstOrDefaultAsync(s => s.Id == signupId);

        if (signup is null)
        {
            return new ActivityChoiceResult(ActivityChoiceOutcome.NotFound);
        }

        if (!signup.IsOpen)
        {
            return new ActivityChoiceResult(ActivityChoiceOutcome.Closed, signup);
        }

        var entry = signup.Entries.FirstOrDefault(e => e.UserId == userId);
        var outcome = ActivityChoiceOutcome.Chosen;

        if (entry is null)
        {
            signup.Entries.Add(new ActivitySignupEntry
            {
                UserId = userId,
                Choice = choice,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else if (entry.Choice == choice)
        {
            signup.Entries.Remove(entry);
            outcome = ActivityChoiceOutcome.Withdrawn;
        }
        else
        {
            entry.Choice = choice;
            entry.UpdatedAtUtc = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        return new ActivityChoiceResult(outcome, signup);
    }

    public async Task CloseDueSignupsAsync(DateTime nowUtc)
    {
        var due = await context.ActivitySignups
                               .Include(s => s.Entries)
                               .Where(s => s.IsOpen && s.CloseAtUtc <= nowUtc)
                               .OrderBy(s => s.SlotStartUtc)
                               .ToListAsync();

        foreach (var signup in due)
        {
            // Closed before talking to Discord, so a failing message can't make it tally twice
            signup.IsOpen = false;
            await context.SaveChangesAsync();

            try
            {
                if (signup.MessageId is { } messageId)
                {
                    await client.EditEmbedMessageAsync(signup.ChannelId, messageId, BuildMessage(signup));
                }

                await client.SendEmbedMessageAsync(
                    signup.ChannelId,
                    new MessageProperties { Embeds = [messageBuilder.BuildTally(signup)] });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to close activity signup {Id}", signup.Id);
            }
        }
    }

    public async Task<DateTime?> GetNextCloseUtcAsync()
    {
        return await context.ActivitySignups
                            .Where(s => s.IsOpen)
                            .OrderBy(s => s.CloseAtUtc)
                            .Select(s => (DateTime?)s.CloseAtUtc)
                            .FirstOrDefaultAsync();
    }

    private async Task<ActivitySignup> PostAsync(ActivitySignup signup)
    {
        // Saved first because the buttons need the signup's id
        await context.ActivitySignups.AddAsync(signup);
        await context.SaveChangesAsync();

        try
        {
            var message = await client.SendEmbedMessageAsync(signup.ChannelId, WithMemberPing(BuildMessage(signup), signup));

            signup.MessageId = message.Id;
            await context.SaveChangesAsync();

            return signup;
        }
        catch
        {
            // Dropped so the next pass posts it again
            context.ActivitySignups.Remove(signup);
            await context.SaveChangesAsync();

            throw;
        }
    }

    // Only the initial post pings, test signups never do
    private MessageProperties WithMemberPing(MessageProperties message, ActivitySignup signup)
    {
        if (signup.IsTest || _memberRoleId == 0)
        {
            return message;
        }

        message.Content = $"<@&{_memberRoleId}>";
        message.AllowedMentions = new AllowedMentionsProperties
        {
            Everyone = false,
            AllowedRoles = [_memberRoleId],
            AllowedUsers = []
        };

        return message;
    }

    private MessageProperties BuildMessage(ActivitySignup signup)
    {
        return new MessageProperties
        {
            Embeds = [messageBuilder.BuildSignupEmbed(signup)],
            Components = messageBuilder.BuildComponents(signup)
        };
    }
}
