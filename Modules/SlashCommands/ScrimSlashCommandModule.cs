using IchigoHoshimiya.DTO;
using IchigoHoshimiya.Entities.General;
using IchigoHoshimiya.Helpers;
using IchigoHoshimiya.Interfaces;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace IchigoHoshimiya.Modules.SlashCommands;

[UsedImplicitly]
public class ScrimSlashCommandModule(IScrimService scrimService, IConfiguration configuration, RestClient restClient)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("scrim", "Create a Where Winds Meet GvG scrim signup")]
    [UsedImplicitly]
    public async Task CreateScrimSignup()
    {
        if (!await EnsureOwnerAsync())
        {
            return;
        }

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        var signup = await scrimService.CreateSignupAsync(Context.Channel.Id, Context.User.Id);

        await Context.Interaction.ModifyResponseAsync(message =>
            message.WithContent($"✅ Scrim signup created in this channel with Id `{signup.Id}` and message Id `{signup.MessageId}`."));
    }

    [SlashCommand("scrimstats", "Show signup stats for a scrim signup message")]
    [UsedImplicitly]
    public async Task ShowScrimStats(
        [SlashCommandParameter(Name = "messageid", Description = "The Discord message ID of the scrim signup post")]
        string messageId)
    {
        if (!await EnsureOwnerAsync())
        {
            return;
        }

        if (!ulong.TryParse(messageId, out var parsedMessageId))
        {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties
                    {
                        Content = "That is not a valid Discord message ID.",
                        Flags = MessageFlags.Ephemeral
                    }));
            return;
        }

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());

        var report = await scrimService.GetStatsByMessageIdAsync(parsedMessageId);

        if (report is null)
        {
            await Context.Interaction.ModifyResponseAsync(message =>
                message.WithContent("That message is not a tracked scrim signup message."));
            return;
        }

        var embeds = BuildStatsEmbeds(report);

        await Context.Interaction.ModifyResponseAsync(message =>
        {
            message.Content = $"Scrim stats for message `{report.MessageId}`";
            message.Embeds = embeds;
        });
    }

    [SlashCommand("pingunsignedscrim", "Ping members of a role who haven't signed up for a scrim")]
    [UsedImplicitly]
    public async Task PingUnsignedScrim(
        [SlashCommandParameter(Name = "messageid", Description = "The Discord message ID of the scrim signup post")]
        string messageId,
        [SlashCommandParameter(Name = "roleid", Description = "The Discord role ID whose members should be pinged if unsigned")]
        string roleId)
    {
        if (!await EnsureOwnerAsync())
        {
            return;
        }

        if (!ulong.TryParse(messageId, out var parsedMessageId))
        {
            await RespondEphemeralAsync("That is not a valid Discord message ID.");
            return;
        }

        if (!ulong.TryParse(roleId, out var parsedRoleId))
        {
            await RespondEphemeralAsync("That is not a valid Discord role ID.");
            return;
        }

        var guildId = Context.Interaction.GuildId;
        if (guildId is null)
        {
            await RespondEphemeralAsync("This command must be used inside a server.");
            return;
        }

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        var signup = await scrimService.GetSignupByMessageIdAsync(parsedMessageId);
        if (signup is null)
        {
            await Context.Interaction.ModifyResponseAsync(m =>
                m.WithContent("That message is not a tracked scrim signup message."));
            return;
        }

        // Collect signed-up user IDs
        var signedUpIds = signup.Entries.Select(e => e.UserId).ToHashSet();

        // Fetch all guild members who have the target role
        var roleMembers = new List<ulong>();
        await foreach (var member in restClient.GetGuildUsersAsync(guildId.Value))
        {
            if (member.RoleIds.Contains(parsedRoleId))
                roleMembers.Add(member.Id);
        }

        if (roleMembers.Count == 0)
        {
            await Context.Interaction.ModifyResponseAsync(m =>
                m.WithContent($"No members found with role `{parsedRoleId}`."));
            return;
        }

        var unsigned = roleMembers.Where(id => !signedUpIds.Contains(id)).ToList();

        if (unsigned.Count == 0)
        {
            await Context.Interaction.ModifyResponseAsync(m =>
                m.WithContent("✅ Everyone from that role has already signed up!"));
            return;
        }

        // Build ping message — Discord message limit is 2000 chars, chunk if needed
        var pings = unsigned.Select(id => $"<@{id}>").ToList();
        var reminder = "Please sign up for the scrim!";

        var chunks = new List<string>();
        var current = new System.Text.StringBuilder();
        foreach (var ping in pings)
        {
            if (current.Length + ping.Length + 1 > 1900)
            {
                chunks.Add(current.ToString());
                current.Clear();
            }
            if (current.Length > 0) current.Append(' ');
            current.Append(ping);
        }
        if (current.Length > 0) chunks.Add(current.ToString());

        // Send the first chunk as the original deferred response followup, rest as followups
        var channelId = Context.Channel.Id;
        await restClient.SendMessageAsync(channelId, new MessageProperties
        {
            Content = $"{chunks[0]}\n{reminder}"
        });

        for (var i = 1; i < chunks.Count; i++)
        {
            await restClient.SendMessageAsync(channelId, new MessageProperties
            {
                Content = chunks[i]
            });
        }

        await Context.Interaction.ModifyResponseAsync(m =>
            m.WithContent($"✅ Pinged {unsigned.Count} unsigned member(s) from that role."));
    }

    private Task RespondEphemeralAsync(string content)
    {
        return Context.Interaction.SendResponseAsync(
            InteractionCallback.Message(new InteractionMessageProperties
            {
                Content = content,
                Flags = MessageFlags.Ephemeral
            }));
    }

    private async Task<bool> EnsureOwnerAsync()
    {
        var ownerUserId = configuration.GetValue<ulong>("Discord:OwnerUserId");

        if (ownerUserId == 0)
        {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties
                    {
                        Content = "`Discord:OwnerUserId` is not configured yet.",
                        Flags = MessageFlags.Ephemeral
                    }));

            return false;
        }

        if (Context.User.Id != ownerUserId)
        {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties
                    {
                        Content = "Only the bot owner can use this command.",
                        Flags = MessageFlags.Ephemeral
                    }));

            return false;
        }

        return true;
    }

    private static IReadOnlyList<EmbedProperties> BuildStatsEmbeds(ScrimStatsReport report)
    {
        var summary = BuildSummaryEmbed(report);
        var dayFields = report.DayStats
            .Select(day => BuildDayFields(day))
            .ToList();

        var embeds = new List<EmbedProperties> { summary };
        var dayFieldPages = ChunkFields(dayFields, 20);

        for (var i = 0; i < dayFieldPages.Count; i++)
        {
            embeds.Add(new EmbedProperties
            {
                Title = i == 0 ? "Scrim Day Breakdown" : $"Scrim Day Breakdown (cont. {i + 1})",
                Color = EmbedHelper.Build().Color,
                Fields = dayFieldPages[i],
                Footer = new EmbedFooterProperties
                {
                    Text = $"Scrim Signup ID: {report.SignupId}"
                },
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        return embeds;
    }

    private static EmbedProperties BuildSummaryEmbed(ScrimStatsReport report)
    {
        var recommendedText = report.RecommendedDays.Count == 0
            ? "No day has any signups yet."
            : string.Join(
                ", ",
                report.RecommendedDays.Select(day =>
                    $"{ScrimSignupHelper.FormatSingleDayShort(day.Day)} ({day.TotalSignups}, H{day.RoleCounts[ScrimRole.Healer]}/T{day.RoleCounts[ScrimRole.Tank]}/D{day.RoleCounts[ScrimRole.Dps]})"));

        var description = string.Join(
            "\n",
            [
                $"**Recommended day(s):** {recommendedText}",
                $"**Unique signups:** {report.TotalUniqueSignups}",
                $"**Legend:** H=Healer, T=Tank, D=DPS"
            ]);

        return new EmbedProperties
        {
            Title = "Scrim Stats Summary",
            Description = description,
            Color = EmbedHelper.Build().Color,
            Footer = new EmbedFooterProperties
            {
                Text = $"Scrim Signup ID: {report.SignupId} • Message ID: {report.MessageId}"
            },
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static EmbedFieldProperties BuildDayFields(ScrimDayStats day)
    {
        return new EmbedFieldProperties
        {
            Name = ScrimSignupHelper.FormatSingleDay(day.Day),
            Value = day.TotalSignups == 0
                ? "No signups."
                : $"Total {day.TotalSignups} | H {day.RoleCounts[ScrimRole.Healer]} | T {day.RoleCounts[ScrimRole.Tank]} | D {day.RoleCounts[ScrimRole.Dps]}"
        };
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
}
