namespace TahaKonsept.Shared.Contracts.Providers;

/// <summary>
/// Katalogdaki bir modelin yeteneği (model listeleri bu alana göre süzülür: <c>api/providers/models?capability=Text</c>).
/// </summary>
public enum ModelCapability
{
    /// <summary>Metin üreten LLM (konsept, konu önerisi, video kodu).</summary>
    Text = 0,

    /// <summary>Görsel üretimi.</summary>
    Image = 1,

    /// <summary>Seslendirme (TTS).</summary>
    Speech = 2,

    /// <summary>Video motoru (HyperFrames, Remotion, Revideo).</summary>
    VideoEngine = 3,
}
