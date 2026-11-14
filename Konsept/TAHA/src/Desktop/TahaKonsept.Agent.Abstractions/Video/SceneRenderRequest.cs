namespace TahaKonsept.Agent.Abstractions.Video;

/// <summary>Bir sahnenin render isteği.</summary>
/// <param name="WorkDir">Sahneye ayrılmış çalışma klasörü (motor içinde proje dosyalarını kurar).</param>
/// <param name="SceneIndex">Sahne sırası (0 tabanlı).</param>
/// <param name="SceneCode">LLM'in ürettiği sahne kodu.</param>
/// <param name="Width">Genişlik (px).</param>
/// <param name="Height">Yükseklik (px).</param>
/// <param name="Fps">Kare hızı.</param>
/// <param name="DurationSec">Sahne süresi (sn).</param>
/// <param name="AssetPaths">Sahnede kullanılacak yerel dosyalar (görseller); kod bunlara dosya adıyla başvurur.</param>
/// <param name="OutputPath">Çıktı .mp4 yolu.</param>
public sealed record SceneRenderRequest(
    string WorkDir,
    int SceneIndex,
    string SceneCode,
    int Width,
    int Height,
    int Fps,
    double DurationSec,
    IReadOnlyList<string> AssetPaths,
    string OutputPath);
