using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Application.Paging;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Diagnostics.Application.Models;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Application.Abstract;

/// <summary>
/// Hata bildirimleri: gönder (herkes, oturumsuz kurulum ekranı dahil), listele ve durum değiştir (yönetici).
/// </summary>
public interface IErrorReportService : IApplicationService
{
    /// <summary>Bildirim oluşturur; günlüğe uyarı düşer. Sürüm boşsa sunucu sürümü yazılır.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Oluşan bildirim.</returns>
    Task<Result<ErrorReportDto>> CreateAsync(ErrorReportCreateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Bildirimleri listeler (yönetici; en yeni önce).</summary>
    /// <param name="query">Filtre.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayfa.</returns>
    Task<Result<PagedResult<ErrorReportDto>>> ListAsync(ErrorReportQuery query, CancellationToken cancellationToken = default);

    /// <summary>Durum değiştirir (yönetici).</summary>
    /// <param name="id">Bildirim.</param>
    /// <param name="request">Yeni durum.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncel bildirim.</returns>
    Task<Result<ErrorReportDto>> ChangeStatusAsync(Guid id, ErrorReportStatusUpdateRequest request, CancellationToken cancellationToken = default);
}
