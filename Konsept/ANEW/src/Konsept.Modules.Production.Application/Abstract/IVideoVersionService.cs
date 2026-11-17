using Konsept.Modules.Production.Application.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Production.Application.Abstract;

/// <summary>
/// Yeni sürüm ve yeniden üret (spec 0020). İsteği yapan kullanıcıyla çalışır; başka kullanıcının videosu "bulunamadı"dır, iş oluşmaz ve ajana
/// komut gitmez (BR-1, Req 14). Online çağrı gerektiren başlatma açık onay ister; tekrarlanan istek ikinci iş oluşturmaz (H-3).
/// </summary>
public interface IVideoVersionService
{
    /// <summary>Yeni sürüm alanı: taban sürümün ayarları ve seçenekler (Req 2).</summary>
    /// <param name="videoId">Video (konsept).</param>
    /// <param name="baseVersionId">Taban sürüm (boşsa en yenisi).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Seçenekler.</returns>
    Task<Result<VersionOptionsDto>> GetOptionsAsync(Guid videoId, Guid? baseVersionId, CancellationToken cancellationToken);

    /// <summary>Çalışacak aşamalar ve online çağrı bilgisi (Req 3).</summary>
    /// <param name="videoId">Video.</param>
    /// <param name="request">Yeni ayarlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Önizleme.</returns>
    Task<Result<VersionPreviewDto>> PreviewAsync(Guid videoId, NewVersionRequest request, CancellationToken cancellationToken);

    /// <summary>Yeni sürümü sıranın sonuna ekler (Req 1, 6, 8).</summary>
    /// <param name="videoId">Video.</param>
    /// <param name="request">Yeni ayarlar ve online onayı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sürüm işi.</returns>
    Task<Result<ActiveVersionDto>> StartAsync(Guid videoId, NewVersionRequest request, CancellationToken cancellationToken);

    /// <summary>Yeniden üret bağlamını açar (Req 4, 15).</summary>
    /// <param name="videoId">Video.</param>
    /// <param name="baseVersionId">Taban sürüm (boşsa en yenisi).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Bağlam.</returns>
    Task<Result<RegenerationContextDto>> OpenRegenerationAsync(Guid videoId, Guid? baseVersionId, CancellationToken cancellationToken);

    /// <summary>Yeniden üretim önizlemesi: tüm aşamalar, online çağrı, konsept değişti mi (Req 3, 5).</summary>
    /// <param name="videoId">Video.</param>
    /// <param name="token">Bağlam belirteci.</param>
    /// <param name="baseVersionId">Taban sürüm.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Önizleme.</returns>
    Task<Result<VersionPreviewDto>> PreviewRegenerationAsync(Guid videoId, string? token, Guid? baseVersionId, CancellationToken cancellationToken);

    /// <summary>Yeniden üretimi sıranın sonuna ekler (Req 4–6).</summary>
    /// <param name="videoId">Video.</param>
    /// <param name="request">Bağlam, "Yine de baştan üret" ve online onayı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sürüm işi.</returns>
    Task<Result<ActiveVersionDto>> StartRegenerationAsync(Guid videoId, RegenerateVersionRequest request, CancellationToken cancellationToken);

    /// <summary>Tekrar dene: hata veren sürüm yarım kalan aşamadan devam eder (Req 10, BR-10).</summary>
    /// <param name="videoId">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sürüm işi.</returns>
    Task<Result<ActiveVersionDto>> RetryAsync(Guid videoId, CancellationToken cancellationToken);

    /// <summary>Sürümü kaldır: hata veren sürüm işi ve yarım çıktıları kaldırılır; önceki sürümler ve harcama kayıtları değişmez (Req 10).</summary>
    /// <param name="videoId">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kaldırılan iş.</returns>
    Task<Result<ActiveVersionDto>> RemoveFailedAsync(Guid videoId, CancellationToken cancellationToken);
}
