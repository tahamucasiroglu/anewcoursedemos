namespace TahaKonsept.Shared.Contracts.Agent.Payloads;

/// <summary>
/// <c>SceneRender</c> aşamasının ajan girdisi (sahne başına bir komut; <c>StageCommand.SceneIndex</c> ile aynı sahne).
/// Ajan önce <c>ValidateSceneAsync</c> (lint) çalıştırır, sonra <c>scenes/scene-&lt;i&gt;.mp4</c> üretir (sessiz, altyazısız).
/// </summary>
public sealed record SceneRenderStagePayload
{
    /// <summary>Video motoru anahtarı (ör. <c>hyperframes</c>).</summary>
    public string EngineKey { get; init; } = "hyperframes";

    /// <summary>Sahne indeksi.</summary>
    public int SceneIndex { get; init; }

    /// <summary>LLM'in ürettiği sahne kodu.</summary>
    public string SceneCode { get; init; } = string.Empty;

    /// <summary>Genişlik (px).</summary>
    public int Width { get; init; } = 1080;

    /// <summary>Yükseklik (px).</summary>
    public int Height { get; init; } = 1920;

    /// <summary>Kare hızı.</summary>
    public int Fps { get; init; } = 30;

    /// <summary>Sahne süresi (sn; ses süresinden).</summary>
    public double DurationSec { get; init; }

    /// <summary>Sahnede kullanılan görsellerin dosya adları (yerel <c>images/</c> ya da indirilen artefaktlar).</summary>
    public IReadOnlyList<string> AssetNames { get; init; } = [];
}
