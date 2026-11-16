using Konsept.Modules.Production.Domain.Entities;

namespace Konsept.Modules.Production.Application.Abstract;

/// <summary>Tamamlanan videoların sürüm kayıtları (spec 0017 Req 20, 22); her işlem kullanıcı kimliğiyle süzülür (BR-1).</summary>
public interface IProducedVideoRepository
{
    /// <summary>Kayıt ekler.</summary>
    /// <param name="video">Kayıt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task AddAsync(ProducedVideo video, CancellationToken cancellationToken);

    /// <summary>Profilin videoları, en yeni üstte.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıtlar.</returns>
    Task<IReadOnlyList<ProducedVideo>> ListAsync(Guid userId, Guid profileId, CancellationToken cancellationToken);

    /// <summary>Kullanıcının videosu.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="videoId">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıt ya da <c>null</c>.</returns>
    Task<ProducedVideo?> GetAsync(Guid userId, Guid videoId, CancellationToken cancellationToken);

    /// <summary>Kullanıcının bir konseptten üretilmiş tüm sürümleri, eskiden yeniye (spec 0007 Req 6).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept (video).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sürümler; başkasının konsepti için boş.</returns>
    Task<IReadOnlyList<ProducedVideo>> ListByConceptAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken);
}
