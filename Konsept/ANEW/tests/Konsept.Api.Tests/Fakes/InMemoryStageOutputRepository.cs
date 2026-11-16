using System.Collections.Concurrent;
using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Domain.Entities;

namespace Konsept.Api.Tests.Fakes;

/// <summary>Aşama çıktılarının bellek deposu (plan 0017); başkasının satırı değişmez.</summary>
public sealed class InMemoryStageOutputRepository : IStageOutputRepository
{
    private readonly ConcurrentDictionary<Guid, StageOutputs> _rows = new();

    /// <inheritdoc />
    public Task<StageOutputs?> GetAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken) =>
        Task.FromResult(_rows.TryGetValue(conceptId, out var o) && o.UserId == userId ? o : null);

    /// <inheritdoc />
    public Task UpsertAsync(StageOutputs outputs, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(outputs);
        _rows.AddOrUpdate(outputs.ConceptId, outputs, (_, old) => old.UserId == outputs.UserId ? outputs : old);
        return Task.CompletedTask;
    }
}
