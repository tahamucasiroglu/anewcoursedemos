using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Domain.Entities;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Infrastructure.Persistence.Concrete;

/// <summary>
/// <see cref="IConceptRepository"/>'nin EF Core uygulaması (kullanıcıya göre filtreli sorgular, durum sayımı).
/// </summary>
/// <param name="context">Modül DbContext'i.</param>
public sealed class ConceptRepository(ConceptsDbContext context) : Repository<Concept>(context), IConceptRepository
{
    /// <summary>Kullanıcının konseptleri (takipsiz, en yeni önce).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="statuses">Durumlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Konseptler.</returns>
    public async Task<IReadOnlyList<Concept>> ListForUserAsync(Guid userId, IReadOnlyCollection<ConceptStatus> statuses, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(statuses);
        var list = statuses.ToList();
        return await QueryNoTracking()
            .Where(c => c.UserId == userId && list.Contains(c.Status))
            .OrderByDescending(c => c.CreatedAtUtc).ThenByDescending(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Kullanıcıya ait konsepti takipli getirir.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Konsept ya da null.</returns>
    public Task<Concept?> GetForUserTrackedAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        Query().FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken);

    /// <summary>Kullanıcıya ait konsepti takipsiz getirir.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Konsept ya da null.</returns>
    public Task<Concept?> GetForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        QueryNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken);

    /// <summary>Duruma göre sayılar (tüm kullanıcılar).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum → sayı.</returns>
    public async Task<IReadOnlyDictionary<ConceptStatus, int>> CountByStatusAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryNoTracking()
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(r => r.Status, r => r.Count);
    }
}
