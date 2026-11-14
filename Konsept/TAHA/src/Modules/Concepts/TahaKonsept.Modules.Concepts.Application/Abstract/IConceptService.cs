using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Oturumdaki kullanıcının konseptlerini yöneten servis (API): liste, ayrıntı, doğrudan düzenleme, silme (Silinenler'e),
/// onaylama, geri alma ve kalıcı silme. Silme/geri alma/kalıcı silme konu hafızasını günceller.
/// </summary>
public interface IConceptService : IApplicationService
{
    /// <summary>Konseptler (durum verilmezse silinmemiş hepsi; en yeni önce).</summary>
    /// <param name="status">Durum.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Liste.</returns>
    Task<Result<IReadOnlyList<ConceptSummaryDto>>> ListAsync(ConceptStatus? status, CancellationToken cancellationToken = default);

    /// <summary>Silinen konseptler (en son silinen önce).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Liste.</returns>
    Task<Result<IReadOnlyList<ConceptSummaryDto>>> ListDeletedAsync(CancellationToken cancellationToken = default);

    /// <summary>Konsept ayrıntısı.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ayrıntı ya da NotFound.</returns>
    Task<Result<ConceptDetailDto>> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Belgeyi doğrudan düzenler.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncel ayrıntı.</returns>
    Task<Result<ConceptDetailDto>> UpdateAsync(Guid id, UpdateConceptRequest request, CancellationToken cancellationToken = default);

    /// <summary>Silinenler'e taşır.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Onaylar (Konseptten Video listesine geçer).</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncel ayrıntı.</returns>
    Task<Result<ConceptDetailDto>> ApproveAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Silinenler'den geri alır.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncel ayrıntı.</returns>
    Task<Result<ConceptDetailDto>> RestoreAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Kalıcı olarak siler (yalnız silinmişler).</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> PurgeAsync(Guid id, CancellationToken cancellationToken = default);
}
