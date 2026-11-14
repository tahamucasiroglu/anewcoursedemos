using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Application.Base;
using Konsept.Modules.Monitoring.Domain.Entities;

namespace Konsept.Modules.Monitoring.Application.Concrete;

/// <summary>Teknik log servisi: kayıtları repository üzerinden yazar ve siler.</summary>
/// <param name="repository">Teknik log repository'si.</param>
/// <param name="timeProvider">Saat kaynağı.</param>
public sealed class TechnicalLogService(ITechnicalLogRepository repository, TimeProvider timeProvider)
    : TechnicalLogServiceBase(timeProvider)
{
    /// <inheritdoc />
    protected override Task PersistAsync(IReadOnlyList<TechnicalLog> logs, CancellationToken cancellationToken) =>
        repository.AddRangeAsync(logs, cancellationToken);

    /// <inheritdoc />
    protected override Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken) =>
        repository.DeleteOlderThanAsync(cutoff, cancellationToken);
}
