namespace TahaKonsept.Shared.Contracts.Agent.Payloads;

/// <summary>
/// <c>Compose</c> aşamasının ajan girdisi: sahneleri birleştir, isteğe bağlı karaoke altyazı ve üst ilerleme çubuğu bas, sesi ekle.
/// Sahne videoları <c>scenes/scene-&lt;i&gt;.mp4</c>, ses <c>speech/speech.wav</c>, altyazı <c>align/captions.json</c> dosyalarından okunur
/// (ya da aynı adlı indirilen artefaktlardan). Çıktı: <c>compose/composed.mp4</c>.
/// </summary>
public sealed record ComposeStagePayload
{
    /// <summary>Genişlik (px).</summary>
    public int Width { get; init; } = 1080;

    /// <summary>Yükseklik (px).</summary>
    public int Height { get; init; } = 1920;

    /// <summary>Kare hızı.</summary>
    public int Fps { get; init; } = 30;

    /// <summary>Sahne hedef süreleri (sıralı; sahne sayısı bu listenin uzunluğudur).</summary>
    public IReadOnlyList<double> SceneDurationsSec { get; init; } = [];

    /// <summary>Karaoke altyazı basılsın mı.</summary>
    public bool BurnSubtitles { get; init; } = true;

    /// <summary>Üstte ilerleme çubuğu olsun mu.</summary>
    public bool ShowProgressBar { get; init; } = true;

    /// <summary>Ses eklensin mi (ses kapalıysa sessiz video).</summary>
    public bool IncludeAudio { get; init; } = true;
}
