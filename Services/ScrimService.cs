using IchigoHoshimiya.Context;
using IchigoHoshimiya.DTO;
using IchigoHoshimiya.Entities.General;
using IchigoHoshimiya.Helpers;
using IchigoHoshimiya.Interfaces;
using Microsoft.EntityFrameworkCore;
using NetCord.Rest;

namespace IchigoHoshimiya.Services;

public class ScrimService(IchigoContext dbContext, RestClient restClient) : IScrimService
{
    private static readonly IReadOnlyList<ScrimRole> Roles = [ScrimRole.Healer, ScrimRole.Tank, ScrimRole.Dps];

    public async Task<ScrimSignup> CreateSignupAsync(ulong channelId, ulong createdById, CancellationToken cancellationToken = default)
    {
        var signup = new ScrimSignup
        {
            ChannelId = channelId,
            CreatedById = createdById
        };

        await dbContext.ScrimSignups.AddAsync(signup, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var message = await restClient.SendMessageAsync(
            channelId,
            new MessageProperties
            {
                Embeds = BuildSignupEmbeds(signup),
                Components = ScrimSignupHelper.BuildPublicComponents(signup.Id)
            },
            cancellationToken: cancellationToken);

        signup.MessageId = message.Id;
        await dbContext.SaveChangesAsync(cancellationToken);

        return signup;
    }

    public Task<ScrimSignup?> GetSignupAsync(long signupId, CancellationToken cancellationToken = default)
    {
        return dbContext.ScrimSignups
            .Include(signup => signup.Entries)
            .AsNoTracking()
            .FirstOrDefaultAsync(signup => signup.Id == signupId, cancellationToken);
    }

    public Task<ScrimSignup?> GetSignupByMessageIdAsync(ulong messageId, CancellationToken cancellationToken = default)
    {
        return dbContext.ScrimSignups
            .Include(signup => signup.Entries)
            .AsNoTracking()
            .FirstOrDefaultAsync(signup => signup.MessageId == messageId, cancellationToken);
    }

    public Task<ScrimSignupEntry?> GetSignupEntryAsync(long signupId, ulong userId, CancellationToken cancellationToken = default)
    {
        return dbContext.ScrimSignupEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(entry => entry.ScrimSignupId == signupId && entry.UserId == userId, cancellationToken);
    }

    public async Task<ScrimSignupEntry> SaveSignupEntryAsync(
        long signupId,
        ulong userId,
        string username,
        ScrimWeapon weapon,
        ScrimAvailableDays availableDays,
        CancellationToken cancellationToken = default)
    {
        var signup = await dbContext.ScrimSignups
            .FirstOrDefaultAsync(x => x.Id == signupId, cancellationToken)
            ?? throw new InvalidOperationException($"Scrim signup {signupId} was not found.");

        if (!signup.IsOpen)
        {
            throw new InvalidOperationException("This scrim signup is closed.");
        }

        if (!ScrimSignupHelper.IsValidWeapon((int)weapon))
        {
            throw new InvalidOperationException("That weapon selection is invalid.");
        }

        var entry = await dbContext.ScrimSignupEntries
            .FirstOrDefaultAsync(x => x.ScrimSignupId == signupId && x.UserId == userId, cancellationToken);

        if (entry is null)
        {
            entry = new ScrimSignupEntry
            {
                ScrimSignupId = signupId,
                UserId = userId,
                Username = username,
                Role = ScrimSignupHelper.GetRoleForWeapon(weapon),
                Weapon = weapon,
                AvailableDays = availableDays,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await dbContext.ScrimSignupEntries.AddAsync(entry, cancellationToken);
        }
        else
        {
            entry.Username = username;
            entry.Role = ScrimSignupHelper.GetRoleForWeapon(weapon);
            entry.Weapon = weapon;
            entry.AvailableDays = availableDays;
            entry.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return entry;
    }

    public async Task<bool> RemoveSignupEntryAsync(long signupId, ulong userId, CancellationToken cancellationToken = default)
    {
        var entry = await dbContext.ScrimSignupEntries
            .FirstOrDefaultAsync(x => x.ScrimSignupId == signupId && x.UserId == userId, cancellationToken);

        if (entry is null)
        {
            return false;
        }

        dbContext.ScrimSignupEntries.Remove(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task RefreshSignupMessageAsync(long signupId, CancellationToken cancellationToken = default)
    {
        var signup = await dbContext.ScrimSignups
            .Include(x => x.Entries)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == signupId, cancellationToken)
            ?? throw new InvalidOperationException($"Scrim signup {signupId} was not found.");

        if (signup.MessageId is null)
        {
            throw new InvalidOperationException($"Scrim signup {signupId} does not have a tracked message yet.");
        }

        await restClient.ModifyMessageAsync(
            signup.ChannelId,
            signup.MessageId.Value,
            options =>
            {
                options.Embeds = BuildSignupEmbeds(signup);
                options.Components = ScrimSignupHelper.BuildPublicComponents(signup.Id);
            },
            cancellationToken: cancellationToken);
    }

    public async Task<ScrimStatsReport?> GetStatsByMessageIdAsync(ulong messageId, CancellationToken cancellationToken = default)
    {
        var signup = await GetSignupByMessageIdAsync(messageId, cancellationToken);

        return signup is null ? null : BuildStatsReport(signup);
    }

    public Task<List<ScrimSignup>> GetExpiredOpenSignupsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return dbContext.ScrimSignups
            .Where(s => s.IsOpen)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ContinueWith(t => t.Result
                .Where(s => GetFridayMidnightUtc(s.CreatedAtUtc) < now)
                .ToList(), cancellationToken);
    }

    public async Task CloseSignupAsync(long signupId, CancellationToken cancellationToken = default)
    {
        var signup = await dbContext.ScrimSignups
            .Include(x => x.Entries)
            .FirstOrDefaultAsync(x => x.Id == signupId, cancellationToken)
            ?? throw new InvalidOperationException($"Scrim signup {signupId} was not found.");

        signup.IsOpen = false;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (signup.MessageId is not null)
        {
            await restClient.ModifyMessageAsync(
                signup.ChannelId,
                signup.MessageId.Value,
                options =>
                {
                    options.Embeds = BuildSignupEmbeds(signup);
                    // Remove interactive components so nobody can sign up any more
                    options.Components = [];
                },
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>Returns the moment Saturday 00:00 UTC of the same ISO week as <paramref name="createdAt"/>.</summary>
    private static DateTime GetFridayMidnightUtc(DateTime createdAt)
    {
        // DayOfWeek.Friday == 5; shift so Monday=0 ... Friday=4 ... Sunday=6
        var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)createdAt.DayOfWeek + 7) % 7;

        // If created on Saturday or Sunday we consider the *following* Friday as the cutoff
        // (those days belong to the next week's scrim cycle).
        if (daysUntilSaturday == 0)
        {
            daysUntilSaturday = 7;
        }

        return createdAt.Date.AddDays(daysUntilSaturday);
    }

    private static IReadOnlyList<EmbedProperties> BuildSignupEmbeds(ScrimSignup signup)
    {
        var groupedEntries = Roles
            .Select(role => new
            {
                Role = role,
                Entries = signup.Entries
                    .Where(entry => ScrimSignupHelper.ResolveRole(entry) == role)
                    .OrderBy(entry => entry.UpdatedAtUtc)
                    .ToList()
            })
            .ToList();

        var allFields = new List<EmbedFieldProperties>();

        foreach (var group in groupedEntries)
        {
            allFields.AddRange(BuildRoleFields(ScrimSignupHelper.FormatRole(group.Role), group.Entries));
        }

        var fieldPages = ChunkFields(allFields, 25);
        var embeds = new List<EmbedProperties>();

        for (var i = 0; i < fieldPages.Count; i++)
        {
            embeds.Add(new EmbedProperties
            {
                Title = i == 0 ? signup.Title : $"{signup.Title} (cont. {i + 1})",
                Description = i == 0
                    ? "Click **Sign Up / Update** to choose your weapon and weekdays. Click **Withdraw** if you can no longer play."
                    : null,
                Color = EmbedHelper.Build().Color,
                Fields = fieldPages[i],
                Footer = new EmbedFooterProperties
                {
                    Text = $"Scrim Signup ID: {signup.Id}"
                },
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        return embeds;
    }

    private static IEnumerable<EmbedFieldProperties> BuildRoleFields(string roleName, IReadOnlyCollection<ScrimSignupEntry> entries)
    {
        if (entries.Count == 0)
        {
            return
            [
                new EmbedFieldProperties
                {
                    Name = $"{roleName} (0)",
                    Value = "No signups yet."
                }
            ];
        }

        var lines = entries
            .Select(entry =>
            {
                var name = string.IsNullOrWhiteSpace(entry.Username) ? entry.UserId.ToString() : entry.Username;
                return $"• {name} — {ScrimSignupHelper.FormatWeaponShort(entry.Weapon)} — {ScrimSignupHelper.FormatDaysShort(entry.AvailableDays)}";
            })
            .ToList();

        var chunks = ChunkLines(lines, 1000);

        return chunks.Select((chunk, index) => new EmbedFieldProperties
        {
            Name = index == 0 ? $"{roleName} ({entries.Count})" : $"{roleName} (cont. {index + 1})",
            Value = string.Join("\n", chunk)
        });
    }

    private static List<List<string>> ChunkLines(IReadOnlyList<string> lines, int maxLength)
    {
        var chunks = new List<List<string>>();
        var currentChunk = new List<string>();
        var currentLength = 0;

        foreach (var line in lines)
        {
            var projectedLength = currentLength == 0 ? line.Length : currentLength + 1 + line.Length;

            if (currentChunk.Count > 0 && projectedLength > maxLength)
            {
                chunks.Add(currentChunk);
                currentChunk = new List<string>();
                currentLength = 0;
            }

            currentChunk.Add(line);
            currentLength = currentLength == 0 ? line.Length : currentLength + 1 + line.Length;
        }

        if (currentChunk.Count > 0)
        {
            chunks.Add(currentChunk);
        }

        return chunks;
    }

    private static List<List<EmbedFieldProperties>> ChunkFields(IReadOnlyList<EmbedFieldProperties> fields, int maxFieldsPerEmbed)
    {
        var pages = new List<List<EmbedFieldProperties>>();
        var currentPage = new List<EmbedFieldProperties>();

        foreach (var field in fields)
        {
            if (currentPage.Count == maxFieldsPerEmbed)
            {
                pages.Add(currentPage);
                currentPage = new List<EmbedFieldProperties>();
            }

            currentPage.Add(field);
        }

        if (currentPage.Count > 0)
        {
            pages.Add(currentPage);
        }

        return pages;
    }

    private static ScrimStatsReport BuildStatsReport(ScrimSignup signup)
    {
        var dayStats = ScrimSignupHelper.Weekdays
            .Select(day => BuildDayStats(signup.Entries, day.Flag))
            .ToList();

        var rankedDays = dayStats
            .Where(day => day.TotalSignups > 0)
            .OrderByDescending(GetRoleCoverage)
            .ThenByDescending(day => day.TotalSignups)
            .ThenByDescending(day => day.RoleCounts[ScrimRole.Healer] + day.RoleCounts[ScrimRole.Tank])
            .ThenBy(day => day.Day)
            .ToList();

        var recommendedDays = new List<ScrimDayStats>();

        if (rankedDays.Count > 0)
        {
            var topDay = rankedDays[0];
            var topCoverage = GetRoleCoverage(topDay);
            var topTotal = topDay.TotalSignups;
            var topSupport = topDay.RoleCounts[ScrimRole.Healer] + topDay.RoleCounts[ScrimRole.Tank];

            recommendedDays = rankedDays
                .Where(day => GetRoleCoverage(day) == topCoverage &&
                              day.TotalSignups == topTotal &&
                              day.RoleCounts[ScrimRole.Healer] + day.RoleCounts[ScrimRole.Tank] == topSupport)
                .ToList();
        }

        return new ScrimStatsReport
        {
            SignupId = signup.Id,
            MessageId = signup.MessageId ?? 0,
            TotalUniqueSignups = signup.Entries.Count,
            DayStats = dayStats,
            RecommendedDays = recommendedDays
        };
    }

    private static ScrimDayStats BuildDayStats(IEnumerable<ScrimSignupEntry> entries, ScrimAvailableDays day)
    {
        var dayEntries = entries
            .Where(entry => entry.AvailableDays.HasFlag(day))
            .ToList();

        var roleCounts = Roles.ToDictionary(
            role => role,
            role => dayEntries.Count(entry => ScrimSignupHelper.ResolveRole(entry) == role));

        var weaponCountsByRole = Roles.ToDictionary(
            role => role,
            role => (IReadOnlyDictionary<ScrimWeapon, int>)dayEntries
                .Where(entry => ScrimSignupHelper.ResolveRole(entry) == role)
                .GroupBy(entry => entry.Weapon)
                .OrderBy(group => ScrimSignupHelper.FormatWeapon(group.Key))
                .ToDictionary(group => group.Key, group => group.Count()));

        return new ScrimDayStats
        {
            Day = day,
            TotalSignups = dayEntries.Count,
            RoleCounts = roleCounts,
            WeaponCountsByRole = weaponCountsByRole,
            DayEntries = dayEntries
        };
    }

    private static int GetRoleCoverage(ScrimDayStats dayStats)
    {
        return dayStats.RoleCounts.Count(pair => pair.Value > 0);
    }
}
