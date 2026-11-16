using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Monitoring.Application.Abstract;

/// <summary>
/// Hata bildirimleri (spec 0022): web bildirimi ve önizlemesi (oturumlu kullanıcı), ajan bildirimi (<see cref="IErrorReportSubmitter"/>),
/// API'nin otomatik kaydı ve admin okuması (<c>admin.panel</c> uçta zorlanır).
/// </summary>
public interface IErrorReportService : IErrorReportSubmitter
{
    /// <summary>Modalın göstereceği otomatik bilgiler (versiyon, zaman, son 20 Günlük kaydı).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Önizleme ya da oturum yok hatası.</returns>
    Task<Result<ErrorReportPreviewDto>> GetPreviewAsync(CancellationToken cancellationToken);

    /// <summary>Web bildirimini kaydeder: doğrulama, saatlik sınır, Günlük kaydı.</summary>
    /// <param name="request">Açıklama, sayfa, korelasyon.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Bildirim numarası ya da hata.</returns>
    Task<Result<ErrorReportReceiptDto>> SubmitWebAsync(WebErrorReportRequest? request, CancellationToken cancellationToken);

    /// <summary>Numarayla bildirimi okur (yalnızca admin; yetki uçta).</summary>
    /// <param name="number">Numara.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Bildirim ya da bulunamadı.</returns>
    Task<Result<ErrorReportDto>> GetAsync(long number, CancellationToken cancellationToken);

    /// <summary>API'nin beklenmeyen hatasını kaynak "API" ile kaydeder (istek oturumluysa kullanıcıyla); sınıra sayılmaz.</summary>
    /// <param name="summary">Hata özeti (maskelenir).</param>
    /// <param name="correlationId">Korelasyon numarası.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task RecordUnexpectedAsync(string summary, string? correlationId, CancellationToken cancellationToken);
}
