using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Agent.Abstractions.Engines.Abstract;

namespace TahaKonsept.Agent.Abstractions.Video.Abstract;

/// <summary>
/// LLM'in yazdığı sahne kodunu (HyperFrames HTML, Remotion/Revideo TSX) <b>sessiz ve altyazısız</b> video dosyasına render eden motor.
/// Altyazı, ilerleme çubuğu ve ses <c>IComposer</c> aşamasında eklenir.
/// </summary>
public interface IVideoEngine : IAgentEngine
{
    /// <summary>Tek sahneyi render eder.</summary>
    /// <param name="request">Sahne kodu ve çıktı ayarları.</param>
    /// <param name="progress">0–100 ilerleme.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Üretilen video yolu ve süresi.</returns>
    Task<Result<VideoRenderOutput>> RenderSceneAsync(SceneRenderRequest request, IProgress<double>? progress, CancellationToken ct);

    /// <summary>Sahne kodunu render etmeden doğrular (lint/check). LLM'e geri bildirim için hata metni döner.</summary>
    /// <param name="request">Sahne isteği.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Geçerliyse başarı; değilse ayrıntılı hata.</returns>
    Task<Result> ValidateSceneAsync(SceneRenderRequest request, CancellationToken ct);
}
