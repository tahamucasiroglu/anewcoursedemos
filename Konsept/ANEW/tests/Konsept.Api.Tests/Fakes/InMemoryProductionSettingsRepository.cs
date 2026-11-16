using System.Collections.Concurrent;
using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Domain.Entities;

namespace Konsept.Api.Tests.Fakes;

/// <summary>Üretim ayarlarının bellek deposu (plan 0006); başka kullanıcının satırı değişmez.</summary>
public sealed class InMemoryProductionSettingsRepository : IProductionSettingsRepository
{
    private readonly ConcurrentDictionary<Guid, ProductionSettings> _rows = new();

    /// <summary>Gets satırlar.</summary>
    public IReadOnlyCollection<ProductionSettings> Rows => [.. _rows.Values];

    /// <inheritdoc />
    public Task<ProductionSettings?> GetAsync(Guid userId, Guid profileId, CancellationToken cancellationToken) =>
        Task.FromResult(_rows.TryGetValue(profileId, out var s) && s.UserId == userId ? s : null);

    /// <inheritdoc />
    public Task UpsertAsync(ProductionSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _rows.AddOrUpdate(settings.ProfileId, settings, (_, old) => old.UserId == settings.UserId ? settings : old);
        return Task.CompletedTask;
    }
}
