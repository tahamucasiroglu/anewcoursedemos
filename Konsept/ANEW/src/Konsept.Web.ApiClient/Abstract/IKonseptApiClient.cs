using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient.Dtos;

namespace Konsept.Web.ApiClient.Abstract;

/// <summary>
/// Web'in API'ye yaptığı çağrılar. API'ye ulaşılamazsa <c>api.unavailable</c> hatası döner.
/// </summary>
public interface IKonseptApiClient
{
    /// <summary>Sistem durumunu getirir.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum ya da hata.</returns>
    Task<Result<SystemStatusResponse>> GetSystemStatusAsync(CancellationToken cancellationToken);

    /// <summary>Ajana test komutu gönderir.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ajan bilgisi ya da hata.</returns>
    Task<Result<AgentTestResponse>> TestAgentAsync(CancellationToken cancellationToken);

    /// <summary>Web'in Warning+ loglarını veritabanındaki teknik loga iletir (servis anahtarıyla).</summary>
    /// <param name="logs">Kayıtlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Yazılan kayıt sayısı ya da hata.</returns>
    Task<Result<int>> ForwardLogsAsync(IReadOnlyList<ForwardedLogRequest> logs, CancellationToken cancellationToken);
}
