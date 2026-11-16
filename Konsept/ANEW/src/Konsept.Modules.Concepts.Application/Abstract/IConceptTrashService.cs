using Konsept.Modules.Concepts.Application.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Silinenler ve önerileri sıfırla (spec 0016): konsepti Silinenler'e taşı, Silinenler listesi ve salt okunur detay, kalıcı sil, profilin
/// önerilip seçilmemiş başlık hafızasını sıfırla. Yalnızca kendi kayıtları (BR-1); online çağrı yok.
/// </summary>
public interface IConceptTrashService
{
    /// <summary>Konsepti Silinenler'e taşır (yalnızca Üretildi/Onaylandı; S-5).</summary>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Taşınan konsept satırı.</returns>
    Task<Result<DeletedConceptItemDto>> DeleteAsync(Guid conceptId, CancellationToken cancellationToken);

    /// <summary>Profilin Silinenler'deki konseptleri.</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="page">Sayfa (1'den).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayfa.</returns>
    Task<Result<DeletedConceptListDto>> ListAsync(Guid profileId, int page, CancellationToken cancellationToken);

    /// <summary>Silinmiş konseptin tamamı (salt okunur; G-15).</summary>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Detay.</returns>
    Task<Result<ConceptDetailDto>> GetAsync(Guid conceptId, CancellationToken cancellationToken);

    /// <summary>Silinmiş konsepti kalıcı siler (geri alınamaz; BR-7).</summary>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Silinen konsept satırı.</returns>
    Task<Result<DeletedConceptItemDto>> PurgeAsync(Guid conceptId, CancellationToken cancellationToken);

    /// <summary>Seçili profilin önerilip seçilmemiş başlık hafızasını siler (G-10).</summary>
    /// <param name="request">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<ResetSuggestionsDto>> ResetSuggestionsAsync(ResetSuggestionsRequest? request, CancellationToken cancellationToken);
}
