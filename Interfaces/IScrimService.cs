using IchigoHoshimiya.DTO;
using IchigoHoshimiya.Entities.General;

namespace IchigoHoshimiya.Interfaces;

public interface IScrimService
{
    Task<ScrimSignup> CreateSignupAsync(ulong channelId, ulong createdById, CancellationToken cancellationToken = default);

    Task<ScrimSignup?> GetSignupAsync(long signupId, CancellationToken cancellationToken = default);

    Task<ScrimSignup?> GetSignupByMessageIdAsync(ulong messageId, CancellationToken cancellationToken = default);

    Task<ScrimSignupEntry?> GetSignupEntryAsync(long signupId, ulong userId, CancellationToken cancellationToken = default);

    Task<ScrimSignupEntry> SaveSignupEntryAsync(
        long signupId,
        ulong userId,
        ScrimWeapon weapon,
        ScrimAvailableDays availableDays,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveSignupEntryAsync(long signupId, ulong userId, CancellationToken cancellationToken = default);

    Task RefreshSignupMessageAsync(long signupId, CancellationToken cancellationToken = default);

    Task<ScrimStatsReport?> GetStatsByMessageIdAsync(ulong messageId, CancellationToken cancellationToken = default);

    Task<List<ScrimSignup>> GetExpiredOpenSignupsAsync(CancellationToken cancellationToken = default);

    Task CloseSignupAsync(long signupId, CancellationToken cancellationToken = default);
}

