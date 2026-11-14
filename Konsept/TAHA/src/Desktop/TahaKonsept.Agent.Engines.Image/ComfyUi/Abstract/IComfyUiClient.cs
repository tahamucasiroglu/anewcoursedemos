using System.Text.Json.Nodes;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Image.ComfyUi.Abstract;

/// <summary>Kullanıcının çalıştırdığı ComfyUI sunucusunun HTTP API'si: <c>/system_stats</c>, <c>/prompt</c>, <c>/history</c>, <c>/view</c>.</summary>
public interface IComfyUiClient
{
    /// <summary>Sunucu yanıt veriyor mu (kısa zaman aşımı).</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Ulaşılabiliyorsa <c>true</c>.</returns>
    Task<bool> IsReachableAsync(CancellationToken ct);

    /// <summary>İş akışını kuyruğa ekler.</summary>
    /// <param name="workflow">API biçiminde iş akışı.</param>
    /// <param name="clientId">İstemci kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><c>prompt_id</c> ya da hata (düğüm hataları dahil).</returns>
    Task<Result<string>> QueuePromptAsync(JsonObject workflow, string clientId, CancellationToken ct);

    /// <summary>Geçmiş kaydını okur.</summary>
    /// <param name="promptId">Kuyruk kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Durum; kayıt henüz yoksa (çalışıyor) <c>null</c>.</returns>
    Task<Result<ComfyUiPromptStatus?>> GetStatusAsync(string promptId, CancellationToken ct);

    /// <summary>Görseli <c>/view</c> ile indirip dosyaya yazar.</summary>
    /// <param name="image">Görsel bilgisi.</param>
    /// <param name="destinationPath">Hedef dosya.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Başarı ya da hata.</returns>
    Task<Result> DownloadImageAsync(ComfyUiImageRef image, string destinationPath, CancellationToken ct);
}
