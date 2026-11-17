using Konsept.Modules.Production.Domain.Entities;

namespace Konsept.Modules.Production.Application.Abstract;

/// <summary>
/// Tamamlanan videoların sürüm kayıtları (spec 0017 Req 20, 22); her işlem kullanıcı kimliğiyle süzülür (BR-1). Silinmiş (Silinenler'deki)
/// sürümler yalnızca <c>…Deleted…</c> metotlarında görünür (spec 0019).
/// </summary>
public interface IProducedVideoRepository
{
    /// <summary>Kayıt ekler.</summary>
    /// <param name="video">Kayıt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task AddAsync(ProducedVideo video, CancellationToken cancellationToken);

    /// <summary>Profilin silinmemiş videoları, en yeni üstte.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıtlar.</returns>
    Task<IReadOnlyList<ProducedVideo>> ListAsync(Guid userId, Guid profileId, CancellationToken cancellationToken);

    /// <summary>Kullanıcının silinmemiş video sürümü.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="videoId">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıt ya da <c>null</c>.</returns>
    Task<ProducedVideo?> GetAsync(Guid userId, Guid videoId, CancellationToken cancellationToken);

    /// <summary>Kullanıcının bir konseptten üretilmiş silinmemiş tüm sürümleri, eskiden yeniye (spec 0007 Req 6).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept (video).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sürümler; başkasının konsepti için boş.</returns>
    Task<IReadOnlyList<ProducedVideo>> ListByConceptAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken);

    /// <summary>Videonun (konseptin) silinmemiş tüm sürümlerine aynı silinme anını yazar (spec 0019 Req 1; tek ifade).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept (video).</param>
    /// <param name="at">Silinme anı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İşaretlenen sürüm sayısı (0 = yok, başkasının ya da zaten silinmiş).</returns>
    Task<int> SoftDeleteAsync(Guid userId, Guid conceptId, DateTimeOffset at, CancellationToken cancellationToken);

    /// <summary>Profilin silinmiş sürümleri (spec 0019 Req 3).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıtlar.</returns>
    Task<IReadOnlyList<ProducedVideo>> ListDeletedAsync(Guid userId, Guid profileId, CancellationToken cancellationToken);

    /// <summary>Kullanıcının silinmiş videosunun sürümleri.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept (video).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sürümler; yoksa, başkasınınsa ya da silinmemişse boş.</returns>
    Task<IReadOnlyList<ProducedVideo>> ListDeletedByConceptAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken);

    /// <summary>
    /// Silinmiş videonun kayıtlarını kalıcı kaldırır (spec 0019 Req 4); aynı işlemde klip izini (konsept, ilk tamamlanma) yazar — admin klip
    /// sayısı düşmez (B-130).
    /// </summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept (video).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kaldırıldıysa <c>true</c>.</returns>
    Task<bool> PurgeAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken);
}
