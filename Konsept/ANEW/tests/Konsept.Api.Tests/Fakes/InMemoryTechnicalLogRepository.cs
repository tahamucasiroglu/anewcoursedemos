using System.Collections.Concurrent;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Domain.Entities;

namespace Konsept.Api.Tests.Fakes;

/// <summary>Hızlı profil testleri için bellekte teknik log deposu.</summary>
public sealed class InMemoryTechnicalLogRepository : ITechnicalLogRepository
{
    private readonly ConcurrentQueue<TechnicalLog> _logs = new();

    /// <summary>Gets yazılan kayıtlar.</summary>
    public IReadOnlyCollection<TechnicalLog> Logs => _logs;

    /// <inheritdoc />
    public Task AddRangeAsync(IReadOnlyList<TechnicalLog> logs, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(logs);
        foreach (var log in logs)
        {
            _logs.Enqueue(log);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken) => Task.FromResult(0);
}
