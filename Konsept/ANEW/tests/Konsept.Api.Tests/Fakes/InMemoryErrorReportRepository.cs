using System.Collections.Concurrent;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Application.Dtos;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;

namespace Konsept.Api.Tests.Fakes;

/// <summary>Hızlı profil testleri için bellekte hata bildirimi deposu (spec 0022, yönetim 0025); numara sıralı atanır.</summary>
public sealed class InMemoryErrorReportRepository : IErrorReportRepository
{
    private readonly ConcurrentQueue<ErrorReport> _reports = new();
    private long _number;

    /// <summary>Gets kaydedilen bildirimler.</summary>
    public IReadOnlyCollection<ErrorReport> Reports => _reports;

    /// <inheritdoc />
    public Task AddAsync(ErrorReport report, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(report);
        report.AssignNumber(Interlocked.Increment(ref _number));
        _reports.Enqueue(report);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<int> CountUserReportsSinceAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken) =>
        Task.FromResult(_reports.Count(r => r.UserId == userId && r.Source != ErrorReportSource.Api && r.CreatedAt >= since));

    /// <inheritdoc />
    public Task<ErrorReport?> GetByNumberAsync(long number, CancellationToken cancellationToken) =>
        Task.FromResult(_reports.FirstOrDefault(r => r.Number == number));

    /// <summary>Gets or sets a value indicating whether sonraki durum güncellemesi hata versin (sunucu hatası durumu).</summary>
    public bool FailUpdates { get; set; }

    /// <inheritdoc />
    public Task<ErrorReportPage> ListAsync(
        ErrorReportSource? source,
        IReadOnlyCollection<ErrorReportStatus>? statuses,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var rows = _reports
            .Where(r => (source is null || r.Source == source) && (statuses is null || statuses.Contains(r.Status)))
            .OrderByDescending(r => r.Number)
            .ToList();
        return Task.FromResult(new ErrorReportPage([.. rows.Skip(skip).Take(take)], rows.Count));
    }

    /// <inheritdoc />
    public Task<bool> UpdateStatusAsync(long number, ErrorReportStatus status, string? note, DateTimeOffset at, CancellationToken cancellationToken)
    {
        if (FailUpdates)
        {
            return Task.FromException<bool>(new InvalidOperationException("UPDATE gizli_tablo"));
        }

        var report = _reports.FirstOrDefault(r => r.Number == number);
        report?.ChangeStatus(status, note, at);
        return Task.FromResult(report is not null);
    }
}
