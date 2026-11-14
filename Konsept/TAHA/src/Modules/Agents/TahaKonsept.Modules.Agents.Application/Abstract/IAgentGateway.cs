using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Application.Abstract;

/// <summary>
/// Diğer modüllerin (Production, Ayarlar önizleme vb.) masaüstü programına erişim kapısı. Kullanıcının çevrimiçi ajanına
/// (birden çok bağlantı varsa Hello göndermiş en yeni bağlantıya) SignalR komutları gönderir. Ajan yoksa <c>agents.offline</c> hatası döner.
/// Önemli metotlar: <see cref="GetOnlineAgentAsync"/>, <see cref="SendStageAsync"/>, <see cref="PreviewSpeechAsync"/> (yanıtı bekler),
/// <see cref="GetVideoUrl"/> (<c>http://lan:port/videos/{id}?k=streamKey</c>). Kayıt: scoped.
/// </summary>
public interface IAgentGateway
{
    /// <summary>Kullanıcının çevrimiçi ajanı var mı (bellekten, hızlı).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <returns>Varsa <c>true</c>.</returns>
    bool IsAgentOnline(Guid userId);

    /// <summary>Kullanıcının çevrimiçi ajanının bilgisi.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ajan bilgisi ya da <c>null</c> (çevrimdışı).</returns>
    Task<AgentDeviceInfo?> GetOnlineAgentAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Aşama komutunu ajana gönderir (<c>IAgentClient.ExecuteStage</c>). Sonuç hub olaylarıyla döner.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="command">Komut.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Gönderim sonucu.</returns>
    Task<Result> SendStageAsync(Guid userId, StageCommand command, CancellationToken cancellationToken = default);

    /// <summary>İşi iptal ettirir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="jobId">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Gönderim sonucu.</returns>
    Task<Result> CancelJobAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>İşi duraklatır.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="jobId">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Gönderim sonucu.</returns>
    Task<Result> PauseJobAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>Video klasörünü ajanın bilgisayarında açtırır.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="videoId">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Gönderim sonucu.</returns>
    Task<Result> OpenFolderAsync(Guid userId, Guid videoId, CancellationToken cancellationToken = default);

    /// <summary>Videoyu sistem oynatıcısında açtırır.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="videoId">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Gönderim sonucu.</returns>
    Task<Result> OpenInSystemPlayerAsync(Guid userId, Guid videoId, CancellationToken cancellationToken = default);

    /// <summary>Videonun yerel dosyalarını sildirir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="videoId">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Gönderim sonucu.</returns>
    Task<Result> DeleteVideoAsync(Guid userId, Guid videoId, CancellationToken cancellationToken = default);

    /// <summary>Ajanın donanım/bileşen bilgisini yeniden göndermesini ister.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Gönderim sonucu.</returns>
    Task<Result> RefreshCapabilitiesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ajanda kısa ses önizlemesi üretir ve <c>SpeechPreviewCompleted</c> yanıtını bekler (zaman aşımı: <paramref name="timeout"/> ya da ayar).
    /// <c>RequestId</c> boşsa atanır. Başarılı sonuçta <c>AudioPath</c> ajanın yerel sunucusundaki yoldur; tam adres için <see cref="GetAgentFileUrl"/>.
    /// </summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="command">Önizleme komutu.</param>
    /// <param name="timeout">Bekleme süresi (opsiyonel).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Önizleme sonucu ya da zaman aşımı/çevrimdışı hatası.</returns>
    Task<Result<SpeechPreviewResult>> PreviewSpeechAsync(Guid userId, SpeechPreviewCommand command, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

    /// <summary>Videonun ajan yerel sunucusundaki oynatma adresi: <c>http://{lan}:{port}/videos/{videoId}?k={streamKey}</c>.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="videoId">Video.</param>
    /// <returns>Adres ya da çevrimdışı/adres bilinmiyor hatası.</returns>
    Result<string> GetVideoUrl(Guid userId, Guid videoId);

    /// <summary>Video küçük resminin adresi: <c>http://{lan}:{port}/thumbs/{videoId}?k={streamKey}</c>.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="videoId">Video.</param>
    /// <returns>Adres ya da hata.</returns>
    Result<string> GetThumbnailUrl(Guid userId, Guid videoId);

    /// <summary>Ajan yerel sunucusundaki göreli bir yol için tam adres (ör. önizleme sesi <c>/previews/{id}.wav</c>).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="relativePath">Göreli yol.</param>
    /// <returns>Adres ya da hata.</returns>
    Result<string> GetAgentFileUrl(Guid userId, string relativePath);
}
