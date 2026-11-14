using TahaKonsept.Agent.Engines.Image.Catalog;
using TahaKonsept.Agent.Engines.Image.StableDiffusionCpp.Base;

namespace TahaKonsept.Agent.Engines.Image.StableDiffusionCpp.Concrete;

/// <summary>
/// stable-diffusion.cpp (master-1094, 2026-11) sd-cli bayrakları. Düşük VRAM: <c>--offload-to-cpu</c>,
/// <c>--flash-attn auto,diffusion=on</c> (eski <c>--diffusion-fa</c> bayrağının yeni yazımı) ve <c>--vae-tiling</c>.
/// </summary>
public sealed class SdCppArgumentBuilder : SdCppArgumentBuilderBase
{
    private static readonly string[] LowVram = ["--offload-to-cpu", "--flash-attn", "auto,diffusion=on", "--vae-tiling"];

    /// <inheritdoc />
    public override IReadOnlyList<string> LowVramArgs => LowVram;

    /// <inheritdoc />
    protected override string FlagFor(ImageModelFileRole role) => role switch
    {
        ImageModelFileRole.DiffusionModel => "--diffusion-model",
        ImageModelFileRole.FullModel => "-m",
        ImageModelFileRole.Llm => "--llm",
        ImageModelFileRole.ClipL => "--clip_l",
        ImageModelFileRole.ClipG => "--clip_g",
        ImageModelFileRole.T5xxl => "--t5xxl",
        ImageModelFileRole.Vae => "--vae",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Bilinmeyen model dosyası rolü."),
    };
}
