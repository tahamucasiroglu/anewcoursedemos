using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Concepts.Domain.Entities;

namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Konu hafızası deposu (profil başına kayıtlar).
/// </summary>
public interface ITopicMemoryRepository : IRepository<TopicMemoryEntry>
{
    /// <summary>Kullanıcının profil hafızası (takipsiz, en yeni önce).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıtlar.</returns>
    Task<IReadOnlyList<TopicMemoryEntry>> ListForProfileAsync(Guid userId, Guid? profileId, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcının profil hafızası (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıtlar.</returns>
    Task<IReadOnlyList<TopicMemoryEntry>> ListForProfileTrackedAsync(Guid userId, Guid? profileId, CancellationToken cancellationToken = default);

    /// <summary>Konsepte bağlı kayıtlar (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıtlar.</returns>
    Task<IReadOnlyList<TopicMemoryEntry>> ListByConceptTrackedAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcının tüm hafıza kayıtları (takipli; sıfırlama için).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıtlar.</returns>
    Task<IReadOnlyList<TopicMemoryEntry>> ListForUserTrackedAsync(Guid userId, CancellationToken cancellationToken = default);
}
