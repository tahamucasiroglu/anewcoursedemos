using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Application.Base;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Contracts.Enums;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Shared.Kernel.Abstract;

namespace Konsept.Modules.Monitoring.Application.Concrete;

/// <summary>Hata bildirimleri: bildirim deposu, olay deposu/servisi, oturumdaki kullanıcı ve ürün versiyonu üzerinden.</summary>
/// <param name="reports">Bildirim deposu.</param>
/// <param name="events">Olay deposu (Günlük okuması).</param>
/// <param name="eventWriter">Olay servisi (Günlük yazımı).</param>
/// <param name="currentUser">İsteği yapan kullanıcı.</param>
/// <param name="version">Ürün versiyonu.</param>
/// <param name="timeProvider">Saat kaynağı.</param>
public sealed class ErrorReportService(
    IErrorReportRepository reports,
    ISystemEventRepository events,
    ISystemEventService eventWriter,
    ICurrentUser currentUser,
    IProductVersionProvider version,
    TimeProvider timeProvider) : ErrorReportServiceBase(timeProvider)
{
    // Günlük süresiz saklanır; "son 20 kayıt" için on yıllık aralık yeterli.
    private static readonly TimeSpan JournalLookback = TimeSpan.FromDays(3650);

    /// <inheritdoc />
    protected override Guid? CurrentUserId => currentUser.UserId;

    /// <inheritdoc />
    protected override string ProductVersion => version.Version;

    /// <inheritdoc />
    protected override Task PersistAsync(ErrorReport report, CancellationToken cancellationToken) => reports.AddAsync(report, cancellationToken);

    /// <inheritdoc />
    protected override Task<ErrorReport?> LoadAsync(long number, CancellationToken cancellationToken) => reports.GetByNumberAsync(number, cancellationToken);

    /// <inheritdoc />
    protected override Task<int> CountSinceAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken) =>
        reports.CountUserReportsSinceAsync(userId, since, cancellationToken);

    /// <inheritdoc />
    protected override Task<IReadOnlyList<SystemEvent>> ReadJournalAsync(Guid userId, int count, CancellationToken cancellationToken)
    {
        var now = TimeProvider.GetUtcNow();
        return events.ListOwnedAsync(userId, now - JournalLookback, now.AddDays(1), null, 0, count, cancellationToken);
    }

    /// <inheritdoc />
    protected override Task WriteSentEventAsync(Guid userId, CancellationToken cancellationToken) =>
        eventWriter.WriteAsync(new SystemEventEntry(SentEventType, SystemEventLevel.Info, SentEventMessage, userId), cancellationToken);
}
