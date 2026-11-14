using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Domain.Entities;

namespace TahaKonsept.Modules.Concepts.Infrastructure.Persistence.Concrete;

/// <summary>
/// <see cref="ITopicMemoryRepository"/>'nin EF Core uygulaması.
/// </summary>
/// <param name="context">Modül DbContext'i.</param>
public sealed class TopicMemoryRepository(ConceptsDbContext context) : Repository<TopicMemoryEntry>(context), ITopicMemoryRepository
{
    /// <summary>Profil hafızası (takipsiz, en yeni önce).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıtlar.</returns>
    public async Task<IReadOnlyList<TopicMemoryEntry>> ListForProfileAsync(Guid userId, Guid? profileId, CancellationToken cancellationToken = default) =>
        await QueryNoTracking()
            .Where(e => e.UserId == userId && e.ProfileId == profileId)
            .OrderByDescending(e => e.UpdatedAtUtc).ThenByDescending(e => e.Id)
            .ToListAsync(cancellationToken);

    /// <summary>Profil hafızası (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıtlar.</returns>
    public async Task<IReadOnlyList<TopicMemoryEntry>> ListForProfileTrackedAsync(Guid userId, Guid? profileId, CancellationToken cancellationToken = default) =>
        await Query().Where(e => e.UserId == userId && e.ProfileId == profileId).ToListAsync(cancellationToken);

    /// <summary>Konsepte bağlı kayıtlar (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıtlar.</returns>
    public async Task<IReadOnlyList<TopicMemoryEntry>> ListByConceptTrackedAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken = default) =>
        await Query().Where(e => e.UserId == userId && e.ConceptId == conceptId).ToListAsync(cancellationToken);

    /// <summary>Kullanıcının tüm kayıtları (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıtlar.</returns>
    public async Task<IReadOnlyList<TopicMemoryEntry>> ListForUserTrackedAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await Query().Where(e => e.UserId == userId).ToListAsync(cancellationToken);
}
