using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.VideoServer.Abstract;

/// <summary>
/// Ajanın yerel ağa açık video sunucusu (Kestrel, varsayılan port 7120, <c>0.0.0.0</c>):
/// <c>GET|HEAD /videos/{videoId}?k=</c> (Range destekli mp4), <c>/thumbs/{videoId}?k=</c> (jpg), <c>/previews/{id}.wav?k=</c> (ses önizleme),
/// <c>/health</c>. <c>k</c> kurulum başına rastgele akış anahtarıdır; yanlışsa 403.
/// </summary>
public interface IVideoServer
{
    /// <summary>Dinlenen port (çalışmıyorsa 0).</summary>
    int Port { get; }

    /// <summary>Çalışıyor mu.</summary>
    bool IsRunning { get; }

    /// <summary>Sunucuyu başlatır (port ayarlardan).</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Sonuç (port kullanımdaysa hata).</returns>
    Task<Result> StartAsync(CancellationToken ct);

    /// <summary>Sunucuyu durdurur.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task StopAsync(CancellationToken ct);
}
