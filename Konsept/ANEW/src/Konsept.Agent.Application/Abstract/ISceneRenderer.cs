using Konsept.Agent.Domain.Entities;

namespace Konsept.Agent.Application.Abstract;

/// <summary>
/// Tek sahnenin kısıtlı ortamda render'ı (spec 0018 Req 1–2, 4): iş klasörü, ağ koruması, Job Object sınırları, Node izinleri ve çıktı kapısı.
/// </summary>
public interface ISceneRenderer
{
    /// <summary>Sahneyi render eder; Durdur'da süreç ağacı sonlandırılır ve <see cref="OperationCanceledException"/> atılır.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">Durdur belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<SceneRenderResult> RenderAsync(SceneRenderRequest request, CancellationToken cancellationToken);
}
