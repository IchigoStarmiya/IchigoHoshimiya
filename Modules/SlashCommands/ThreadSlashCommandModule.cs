using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace IchigoHoshimiya.Modules.SlashCommands;

[UsedImplicitly]
public class ThreadSlashCommandModule(RestClient restClient, IConfiguration configuration)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    // Small delay between batches to avoid hitting rate limits
    private const int BatchSize = 10;
    private static readonly TimeSpan BatchDelay = TimeSpan.FromMilliseconds(500);

    [SlashCommand("addroletothread", "Add all members of a role to this thread")]
    [UsedImplicitly]
    public async Task AddRoleToThread(
        [SlashCommandParameter(Name = "roleid", Description = "The ID of the role whose members should be added")]
        string roleIdString)
    {
        if (!await EnsureOwnerAsync())
            return;

        if (!ulong.TryParse(roleIdString, out var roleId))
        {
            await RespondEphemeralAsync("That is not a valid role ID.");
            return;
        }

        var guildId = Context.Interaction.GuildId;
        if (guildId is null)
        {
            await RespondEphemeralAsync("This command must be used inside a server.");
            return;
        }

        var channelId = Context.Channel.Id;

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        // 1. Fetch all guild members who have the role
        var roleMembers = await GetMembersWithRoleAsync(guildId.Value, roleId);

        if (roleMembers.Count == 0)
        {
            await Context.Interaction.ModifyResponseAsync(m =>
                m.WithContent($"No members found with role `{roleId}`."));
            return;
        }

        // 2. Fetch current thread members so we can skip people already in the thread
        HashSet<ulong> alreadyInThread;
        try
        {
            alreadyInThread = await GetThreadMemberIdsAsync(channelId);
        }
        catch
        {
            // Channel might not be a thread — we'll still attempt adds and report failures
            alreadyInThread = [];
        }

        var toAdd = roleMembers
            .Where(id => !alreadyInThread.Contains(id))
            .ToList();

        if (toAdd.Count == 0)
        {
            await Context.Interaction.ModifyResponseAsync(m =>
                m.WithContent($"All {roleMembers.Count} member(s) with that role are already in this thread."));
            return;
        }

        // 3. Add members in batches, silently skip anyone who can't be added (no channel access etc.)
        var added = 0;
        var skippedNoAccess = 0;

        for (var i = 0; i < toAdd.Count; i += BatchSize)
        {
            var batch = toAdd.Skip(i).Take(BatchSize).ToList();

            foreach (var userId in batch)
            {
                try
                {
                    await restClient.AddGuildThreadUserAsync(channelId, userId);
                    added++;
                }
                catch
                {
                    skippedNoAccess++;
                }
            }

            if (i + BatchSize < toAdd.Count)
                await Task.Delay(BatchDelay);
        }

        var lines = new List<string>
        {
            $"✅ Done. {added} member(s) added to this thread."
        };

        if (alreadyInThread.Count > 0)
            lines.Add($"↩️ {roleMembers.Count - toAdd.Count} member(s) were already here and skipped.");

        if (skippedNoAccess > 0)
            lines.Add($"⚠️ {skippedNoAccess} member(s) could not be added (no channel access).");

        await Context.Interaction.ModifyResponseAsync(m =>
            m.WithContent(string.Join('\n', lines)));
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<List<ulong>> GetMembersWithRoleAsync(ulong guildId, ulong roleId)
    {
        var result = new List<ulong>();

        // GetGuildUsersAsync streams all members; each member exposes Id and RoleIds directly
        await foreach (var member in restClient.GetGuildUsersAsync(guildId))
        {
            if (member.RoleIds.Contains(roleId))
                result.Add(member.Id);
        }

        return result;
    }

    private async Task<HashSet<ulong>> GetThreadMemberIdsAsync(ulong threadId)
    {
        var result = new HashSet<ulong>();

        // GetGuildThreadUsersAsync(threadId, paginationProperties?) — ThreadUser.Id is the user's ID
        await foreach (var member in restClient.GetGuildThreadUsersAsync(threadId))
        {
            result.Add(member.Id);
        }

        return result;
    }


    private async Task<bool> EnsureOwnerAsync()
    {
        var ownerUserId = configuration.GetValue<ulong>("Discord:OwnerUserId");

        if (ownerUserId == 0)
        {
            await RespondEphemeralAsync("`Discord:OwnerUserId` is not configured.");
            return false;
        }

        if (Context.User.Id != ownerUserId)
        {
            await RespondEphemeralAsync("Only the bot owner can use this command.");
            return false;
        }

        return true;
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
}
