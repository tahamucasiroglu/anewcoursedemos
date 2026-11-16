using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Domain.Entities;

namespace Konsept.Api.Tests.Fakes;

/// <summary>Bellek harcama deposu (plan 0013): kayıtlar listede; toplam kullanıcı ve [başlangıç, bitiş) ile süzülür.</summary>
public sealed class InMemorySpendRepository : ISpendRepository
{
    private readonly Lock _gate = new();
    private readonly List<SpendRecord> _records = [];

    /// <summary>Gets kayıtlar.</summary>
    public IReadOnlyList<SpendRecord> Records
    {
        get
        {
            lock (_gate)
            {
                return [.. _records];
            }
        }
    }

    /// <inheritdoc />
    public Task AddAsync(SpendRecord record, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _records.Add(record);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<decimal> SumAsync(Guid userId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_records.Where(r => r.UserId == userId && r.OccurredAt >= fromUtc && r.OccurredAt < toUtc).Sum(r => r.CostUsd));
        }
    }
}
