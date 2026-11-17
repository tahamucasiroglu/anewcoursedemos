using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Monitoring.Application.Abstract;

/// <summary>Admin paneli Hata bildirimleri (spec 0025): liste, detay, durum değiştirme. Yetki (<c>admin.panel</c>) uçtadır; silme yoktur.</summary>
public interface IAdminErrorReportService
{
    /// <summary>Bildirimlerin bir sayfası (varsayılan: açık bildirimler, en yeni üstte, 25'erli).</summary>
    /// <param name="request">Sayfa, kaynak, durum.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayfa.</returns>
    Task<Result<AdminErrorReportPageDto>> ListAsync(AdminErrorReportsRequest? request, CancellationToken cancellationToken);

    /// <summary>Bir bildirimin detayı.</summary>
    /// <param name="number">Numara.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Detay ya da bulunamadı.</returns>
    Task<Result<AdminErrorReportDetailDto>> GetAsync(long number, CancellationToken cancellationToken);

    /// <summary>Durumu (ve isteğe bağlı notu) değiştirir; geçersiz durum / uzun not 400, yok 404 (durum değişmez).</summary>
    /// <param name="number">Numara.</param>
    /// <param name="request">Durum ve not.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncel detay ya da hata.</returns>
    Task<Result<AdminErrorReportDetailDto>> ChangeStatusAsync(long number, ErrorReportStatusRequest? request, CancellationToken cancellationToken);
}
