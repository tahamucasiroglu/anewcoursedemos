using TahaKonsept.Agent.Engines.Image.StableDiffusionCpp.Abstract;
using TahaKonsept.Agent.Engines.Image.StableDiffusionCpp.Base;

namespace TahaKonsept.Agent.Engines.Image.StableDiffusionCpp.Concrete;

/// <summary>
/// stable-diffusion.cpp (commit 3bad892, <c>sd-cli --help</c> ile doğrulandı) bellek bayrakları. Standart = argüman üreticisinin
/// <see cref="ISdCppArgumentBuilder.LowVramArgs"/>'ı; azaltılmış = <c>--backend te=cpu --flash-attn auto,diffusion=on --vae-tiling --max-vram N</c>;
/// en agresif = <c>--backend te=cpu,vae=cpu ... --max-vram N</c> (eski <c>--clip-on-cpu</c>/<c>--vae-on-cpu</c> artık <c>--backend</c> ile yazılıyor).
/// </summary>
public sealed class SdCppMemoryPlanner : SdCppMemoryPlannerBase
{
    private readonly IReadOnlyList<string> _standard;

    /// <summary>Oluşturur.</summary>
    /// <param name="argumentBuilder">Argüman üreticisi (standart düşük VRAM bayrakları).</param>
    public SdCppMemoryPlanner(ISdCppArgumentBuilder argumentBuilder)
    {
        ArgumentNullException.ThrowIfNull(argumentBuilder);
        _standard = argumentBuilder.LowVramArgs;
    }

    /// <inheritdoc />
    protected override IReadOnlyList<string> StandardArgs => _standard;

    /// <inheritdoc />
    protected override IReadOnlyList<string> ReducedArgs(double budgetGiB) =>
        ["--backend", "te=cpu", "--flash-attn", "auto,diffusion=on", "--vae-tiling", "--max-vram", FormatGiB(budgetGiB)];

    /// <inheritdoc />
    protected override IReadOnlyList<string> MinimalArgs(double budgetGiB) =>
        ["--backend", "te=cpu,vae=cpu", "--flash-attn", "auto,diffusion=on", "--vae-tiling", "--max-vram", FormatGiB(budgetGiB)];
}
