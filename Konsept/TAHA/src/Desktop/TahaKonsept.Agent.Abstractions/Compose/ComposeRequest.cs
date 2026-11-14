using TahaKonsept.Agent.Abstractions.Align;

namespace TahaKonsept.Agent.Abstractions.Compose;

/// <summary>Birleştirme isteği.</summary>
/// <param name="WorkDir">Geçici dosyalar için klasör.</param>
/// <param name="ScenePaths">Sıralı sahne videoları.</param>
/// <param name="SceneDurationsSec">Her sahnenin hedef süresi (ses uzunluğundan); kısa sahne son kareyle uzatılır.</param>
/// <param name="AudioPath">Seslendirme dosyası (yoksa sessiz).</param>
/// <param name="Captions">Kelime zamanlamaları (yazılış metniyle); altyazı için.</param>
/// <param name="BurnSubtitles">Karaoke altyazı basılsın mı.</param>
/// <param name="ShowProgressBar">Üstte ilerleme çubuğu olsun mu.</param>
/// <param name="Width">Genişlik.</param>
/// <param name="Height">Yükseklik.</param>
/// <param name="Fps">Kare hızı.</param>
/// <param name="OutputPath">Çıktı .mp4.</param>
public sealed record ComposeRequest(
    string WorkDir,
    IReadOnlyList<string> ScenePaths,
    IReadOnlyList<double> SceneDurationsSec,
    string? AudioPath,
    IReadOnlyList<CaptionWord> Captions,
    bool BurnSubtitles,
    bool ShowProgressBar,
    int Width,
    int Height,
    int Fps,
    string OutputPath);
