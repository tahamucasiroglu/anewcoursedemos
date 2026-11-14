using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Tests.Support;

/// <summary>
/// Test ayar kalıpları: yerel ses + görsel kapalı (varsayılan) ve online ses + online görsel.
/// </summary>
public static class TestSettings
{
    /// <summary>Yerel Supertonic, görsel kapalı, HyperFrames.</summary>
    public static ProductionSettingsDto LocalSpeech { get; } = new()
    {
        CodeModelKey = "claude-opus-5-5",
        VideoEngine = "hyperframes",
        ImageMode = MediaMode.Off,
        SpeechMode = MediaMode.Local,
        SpeechModelKey = "supertonic-3",
        VoiceId = "M1",
        SpeechSpeed = 1.0,
        BurnSubtitles = true,
        ShowProgressBar = true,
        Fps = 30,
    };

    /// <summary>Online ses (Gemini) + online görsel (Nano Banana).</summary>
    public static ProductionSettingsDto OnlineMedia { get; } = LocalSpeech with
    {
        ImageMode = MediaMode.Online,
        ImageModelKey = "gemini-nano-banana-2.1",
        SpeechMode = MediaMode.Online,
        SpeechModelKey = "gemini-3.8-flash-tts",
        VoiceId = "Kore",
    };
}
