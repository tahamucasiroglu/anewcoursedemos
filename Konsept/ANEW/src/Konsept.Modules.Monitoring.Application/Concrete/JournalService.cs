using Konsept.Modules.Identity.Contracts.Abstract;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Application.Base;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;
using Konsept.Shared.Kernel.Abstract;

namespace Konsept.Modules.Monitoring.Application.Concrete;

/// <summary>Günlük geçmişi: olay deposu, oturumdaki kullanıcı ve hesap tarihi (Identity.Contracts) üzerinden.</summary>
/// <param name="events">Olay deposu.</param>
/// <param name="accounts">Hesap bilgisi okuyucusu.</param>
/// <param name="currentUser">İsteği yapan kullanıcı.</param>
/// <param name="timeProvider">Saat kaynağı.</param>
public sealed class JournalService(
    ISystemEventRepository events,
    IUserAccountReader accounts,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : JournalServiceBase(timeProvider)
{
    /// <inheritdoc />
    protected override Guid? CurrentUserId => currentUser.UserId;

    /// <inheritdoc />
    protected override Task<DateTimeOffset?> ReadRegisteredAtAsync(Guid userId, CancellationToken cancellationToken) =>
        accounts.GetRegisteredAtAsync(userId, cancellationToken);

    /// <inheritdoc />
    protected override Task<DateTimeOffset?> ReadEarliestAsync(Guid userId, CancellationToken cancellationToken) =>
        events.GetEarliestOwnedAsync(userId, cancellationToken);

    /// <inheritdoc />
    protected override Task<IReadOnlyList<SystemEvent>> ListAsync(
        Guid userId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        IReadOnlyList<EventLevel>? levels,
        int skip,
        int take,
        CancellationToken cancellationToken) =>
        events.ListOwnedAsync(userId, fromUtc, toUtc, levels, skip, take, cancellationToken);
}
