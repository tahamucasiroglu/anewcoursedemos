using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Application.Dtos;
using Konsept.Modules.Monitoring.Domain.Enums;

namespace Konsept.Api.Tests.Fakes;

/// <summary>Bellek olay ve hata bildirimi depolarından admin sorguları (hızlı profil; gerçek sorgu entegrasyon testinde).</summary>
/// <param name="events">Bellek olay deposu.</param>
/// <param name="reports">Bellek hata bildirimi deposu.</param>
public sealed class InMemoryAdminActivityRepository(InMemorySystemEventRepository events, InMemoryErrorReportRepository reports) : IAdminActivityRepository
{
    /// <summary>Gets or sets a value indicating whether her sorgu hata versin (sunucu hatası durumu).</summary>
    public bool Fail { get; set; }

    /// <inheritdoc />
    public Task<int> CountOpenErrorReportsAsync(CancellationToken cancellationToken) =>
        Fail ? Task.FromException<int>(new InvalidOperationException("SELECT gizli_tablo")) : Task.FromResult(reports.Reports.Count(r => r.Status == ErrorReportStatus.New));

    /// <inheritdoc />
    public Task<IReadOnlyList<UserEventTime>> ListUserEventTimesAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<UserEventTime>>([.. events.Events
            .Where(e => e.UserId != null && e.OccurredAt >= fromUtc && e.OccurredAt < toUtc)
            .Select(e => new UserEventTime(e.UserId!.Value, e.OccurredAt))]);

    /// <inheritdoc />
    public Task<ActivityPage> ListActivitiesAsync(
        IReadOnlyCollection<Guid>? userIds,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        if (Fail)
        {
            return Task.FromException<ActivityPage>(new InvalidOperationException("SELECT gizli_tablo"));
        }

        var rows = events.Events
            .Where(e => e.UserId is { } id && (userIds is null || userIds.Contains(id)))
            .Where(e => (fromUtc is null || e.OccurredAt >= fromUtc) && (toUtc is null || e.OccurredAt < toUtc))
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.Id)
            .ToList();
        return Task.FromResult(new ActivityPage([.. rows.Skip(skip).Take(take)], rows.Count));
    }
}
