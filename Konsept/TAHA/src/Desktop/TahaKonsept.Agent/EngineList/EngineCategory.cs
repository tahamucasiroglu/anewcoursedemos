namespace TahaKonsept.Agent.EngineList;

/// <summary>Durum penceresindeki motor grubu.</summary>
public enum EngineCategory
{
    /// <summary>Video render motoru (HyperFrames, Remotion, Revideo).</summary>
    Video = 0,

    /// <summary>Yerel seslendirme motoru (Supertonic, Chatterbox…).</summary>
    Speech = 1,

    /// <summary>Yerel görsel motoru (stable-diffusion.cpp, ComfyUI).</summary>
    Image = 2,
}
