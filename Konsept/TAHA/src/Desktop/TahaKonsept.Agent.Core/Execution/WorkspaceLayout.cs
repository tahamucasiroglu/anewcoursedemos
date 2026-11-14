using System.Globalization;
using TahaKonsept.Shared.Contracts.Agent.Payloads;

namespace TahaKonsept.Agent.Core.Execution;

/// <summary>
/// İş çalışma alanı (<c>Workspace/&lt;jobId&gt;/</c>) düzeni: <c>inputs/</c> (indirilen artefaktlar), <c>images/</c>, <c>speech/</c>,
/// <c>align/</c>, <c>scenes/&lt;i&gt;/</c>, <c>compose/</c>, <c>final/</c>. Her aşama klasöründe <c>.input-hash</c> tutulur;
/// hash değişirse klasör temizlenir (eski çıktı kullanılmaz), aynıysa var olan çıktılar atlanır (devam/kontrol noktası).
/// </summary>
public static class WorkspaceLayout
{
    /// <summary>İndirilen artefaktlar.</summary>
    public const string InputsDir = "inputs";

    /// <summary>Görseller.</summary>
    public const string ImagesDir = "images";

    /// <summary>Sesler.</summary>
    public const string SpeechDir = "speech";

    /// <summary>Hizalama.</summary>
    public const string AlignDir = "align";

    /// <summary>Sahne videoları (alt klasör: sahne indeksi).</summary>
    public const string ScenesDir = "scenes";

    /// <summary>Birleştirme.</summary>
    public const string ComposeDir = "compose";

    /// <summary>Son aşama (Mux) işaret klasörü.</summary>
    public const string FinalDir = "final";

    /// <summary>Ses önizlemeleri (Workspace altında, işlerden bağımsız).</summary>
    public const string PreviewsDir = "_previews";

    /// <summary>Aşama girdi hash işaret dosyası.</summary>
    public const string InputHashFile = ".input-hash";

    /// <summary>Sahne klasörü.</summary>
    /// <param name="jobDir">İş klasörü.</param>
    /// <param name="sceneIndex">Sahne indeksi.</param>
    /// <returns>Yol.</returns>
    public static string SceneDir(string jobDir, int sceneIndex) =>
        Path.Combine(jobDir, ScenesDir, sceneIndex.ToString(CultureInfo.InvariantCulture));

    /// <summary>Sahne videosu yolu.</summary>
    /// <param name="jobDir">İş klasörü.</param>
    /// <param name="sceneIndex">Sahne indeksi.</param>
    /// <returns>Yol.</returns>
    public static string SceneVideoPath(string jobDir, int sceneIndex) =>
        Path.Combine(SceneDir(jobDir, sceneIndex), AgentArtifactNames.SceneVideo(sceneIndex));

    /// <summary>Birleşik ses yolu.</summary>
    /// <param name="jobDir">İş klasörü.</param>
    /// <returns>Yol.</returns>
    public static string SpeechAudioPath(string jobDir) => Path.Combine(jobDir, SpeechDir, AgentArtifactNames.SpeechAudio);

    /// <summary>Altyazı JSON yolu.</summary>
    /// <param name="jobDir">İş klasörü.</param>
    /// <returns>Yol.</returns>
    public static string CaptionsPath(string jobDir) => Path.Combine(jobDir, AlignDir, AgentArtifactNames.Captions);

    /// <summary>Birleşik video yolu.</summary>
    /// <param name="jobDir">İş klasörü.</param>
    /// <returns>Yol.</returns>
    public static string ComposedVideoPath(string jobDir) => Path.Combine(jobDir, ComposeDir, AgentArtifactNames.ComposedVideo);

    /// <summary>Birleştirme küçük resmi yolu.</summary>
    /// <param name="jobDir">İş klasörü.</param>
    /// <returns>Yol.</returns>
    public static string ComposedThumbnailPath(string jobDir) => Path.Combine(jobDir, ComposeDir, AgentArtifactNames.Thumbnail);
}
