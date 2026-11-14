namespace TahaKonsept.Agent.Engines.Image.StableDiffusionCpp;

/// <summary>sd-cli bellek kipi (boş VRAM'e göre seçilir; CUDA OOM'da bir kez <see cref="Minimal"/> ile yeniden denenir).</summary>
public enum SdCppMemoryLevel
{
    /// <summary>Yeterli boş VRAM: standart düşük VRAM bayrakları (<c>--offload-to-cpu</c>, difüzyonda flash attention, VAE döşeme).</summary>
    Standard,

    /// <summary>Az boş VRAM: metin kodlayıcı CPU'da, VAE döşeme, flash attention, <c>--max-vram</c> bütçesiyle otomatik katman bölme.</summary>
    Reduced,

    /// <summary>En agresif: metin kodlayıcı ve VAE CPU'da, düşük <c>--max-vram</c> bütçesi (yavaş ama sığar).</summary>
    Minimal,
}
