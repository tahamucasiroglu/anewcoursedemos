using TahaKonsept.Agent.Engines.Image.Catalog;

namespace TahaKonsept.Agent.Engines.Image.StableDiffusionCpp.Abstract;

/// <summary>
/// Boş VRAM ve model boyutuna göre sd-cli bellek kipini (<see cref="SdCppMemoryLevel"/>) ve gerçek bayrak adlarını seçer
/// (<c>--offload-to-cpu</c>, <c>--backend te=cpu[,vae=cpu]</c>, <c>--vae-tiling</c>, <c>--flash-attn</c>, <c>--max-vram</c>).
/// Önemli metot: <see cref="Plan"/>.
/// </summary>
public interface ISdCppMemoryPlanner
{
    /// <summary>Planı üretir.</summary>
    /// <param name="model">Model.</param>
    /// <param name="freeVramMiB">Boş VRAM (bilinmiyorsa <c>null</c>).</param>
    /// <param name="lowVramDefault">Boş VRAM bilinmiyorken / yeterliyken standart düşük VRAM bayrakları eklensin mi.</param>
    /// <param name="aggressive">CUDA OOM sonrası en agresif kip mi.</param>
    /// <returns>Plan.</returns>
    SdCppMemoryPlan Plan(ImageModelDefinition model, int? freeVramMiB, bool lowVramDefault, bool aggressive);
}
