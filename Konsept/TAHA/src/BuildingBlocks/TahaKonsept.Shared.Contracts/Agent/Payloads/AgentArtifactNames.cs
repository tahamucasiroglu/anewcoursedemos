using System.Globalization;

namespace TahaKonsept.Shared.Contracts.Agent.Payloads;

/// <summary>
/// Ajan çalışma alanındaki standart artefakt adları. Sunucu online ürettiği dosyaları bu adlarla gönderirse ajan
/// yerel üretilmiş gibi kullanır (ör. online TTS → <see cref="SpeechAudio"/>).
/// </summary>
public static class AgentArtifactNames
{
    /// <summary>Birleşik seslendirme dosyası.</summary>
    public const string SpeechAudio = "speech.wav";

    /// <summary>Kelime zamanlamaları (JSON).</summary>
    public const string Captions = "captions.json";

    /// <summary>Birleştirilmiş (altyazı + ilerleme çubuğu + ses) video.</summary>
    public const string ComposedVideo = "composed.mp4";

    /// <summary>Küçük resim.</summary>
    public const string Thumbnail = "thumb.jpg";

    /// <summary>Sahne videosu adı.</summary>
    /// <param name="sceneIndex">Sahne indeksi.</param>
    /// <returns>Ör. <c>scene-3.mp4</c>.</returns>
    public static string SceneVideo(int sceneIndex) => "scene-" + sceneIndex.ToString(CultureInfo.InvariantCulture) + ".mp4";

    /// <summary>Sahne ses dosyası adı.</summary>
    /// <param name="sceneIndex">Sahne indeksi.</param>
    /// <returns>Ör. <c>scene-3.wav</c>.</returns>
    public static string SceneAudio(int sceneIndex) => "scene-" + sceneIndex.ToString(CultureInfo.InvariantCulture) + ".wav";
}
