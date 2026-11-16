using Konsept.Modules.Concepts.Application.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Concepts.Application.Abstract;

/// <summary>Üretilenler ve onay (spec 0014 Req 22–25): liste, detay, onayla, onayı geri al. Yalnızca kendi konseptleri (BR-1).</summary>
public interface IConceptService
{
    /// <summary>Profilin konseptleri.</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="statuses">Virgüllü durum kodları; boş → Üretildi + Onaylandı.</param>
    /// <param name="page">Sayfa (1'den).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayfa.</returns>
    Task<Result<ConceptListDto>> ListAsync(Guid profileId, string? statuses, int page, CancellationToken cancellationToken);

    /// <summary>Konseptin tamamı.</summary>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Detay.</returns>
    Task<Result<ConceptDetailDto>> GetAsync(Guid conceptId, CancellationToken cancellationToken);

    /// <summary>Onaylar (yalnızca "Üretildi").</summary>
    /// <param name="request">Kimlikler.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Değişen konseptler.</returns>
    Task<Result<IReadOnlyList<ConceptListItemDto>>> ApproveAsync(ConceptIdsRequest? request, CancellationToken cancellationToken);

    /// <summary>Onayı geri alır (yalnızca "Onaylandı").</summary>
    /// <param name="request">Kimlikler.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Değişen konseptler.</returns>
    Task<Result<IReadOnlyList<ConceptListItemDto>>> UnapproveAsync(ConceptIdsRequest? request, CancellationToken cancellationToken);
}
