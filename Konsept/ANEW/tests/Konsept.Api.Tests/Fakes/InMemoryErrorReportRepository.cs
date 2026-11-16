using System.Collections.Concurrent;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;

namespace Konsept.Api.Tests.Fakes;

/// <summary>Hızlı profil testleri için bellekte hata bildirimi deposu (spec 0022); numara sıralı atanır.</summary>
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
}
