namespace TahaKonsept.Shared.Contracts.Agent;

/// <summary>
/// Ajanda kurulu bileşen türü.
/// </summary>
public enum ComponentKind
{
    /// <summary>Ön gereksinim (Node, FFmpeg, Chrome Headless Shell, uv/Python...).</summary>
    Prerequisite = 0,

    /// <summary>Yerel ses motoru.</summary>
    SpeechEngine = 1,

    /// <summary>Yerel ses modeli dosyası.</summary>
    SpeechModel = 2,

    /// <summary>Yerel görsel motoru (stable-diffusion.cpp, ComfyUI).</summary>
    ImageEngine = 3,

    /// <summary>Yerel görsel modeli dosyası.</summary>
    ImageModel = 4,

    /// <summary>Video motoru (HyperFrames, Remotion, Revideo).</summary>
    VideoEngine = 5,

    /// <summary>Diğer.</summary>
    Other = 6,
}
