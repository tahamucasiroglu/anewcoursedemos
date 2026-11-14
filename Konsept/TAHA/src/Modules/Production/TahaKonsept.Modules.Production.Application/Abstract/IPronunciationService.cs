using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Okunuş sözlüğü servisi (<c>api/production/pronunciations...</c>): liste, ekle/güncelle/onayla, sil, önizleme (normalize + ses: yerel → ajan, online → sağlayıcı).
/// </summary>
public interface IPronunciationService
{
    /// <summary>Kayıtlar (öneriler dahil).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<IReadOnlyList<PronunciationDto>>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Ekler/günceller.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<PronunciationDto>> SaveAsync(SavePronunciationRequest request, CancellationToken cancellationToken);

    /// <summary>Siler.</summary>
    /// <param name="id">Kayıt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Önizleme.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<PronunciationPreviewResponse>> PreviewAsync(PronunciationPreviewRequest request, CancellationToken cancellationToken);
}
