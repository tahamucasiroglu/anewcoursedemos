using System.Globalization;
using TahaKonsept.Agent.Abstractions.Image;
using TahaKonsept.Agent.Engines.Image.Catalog;
using TahaKonsept.Agent.Engines.Image.StableDiffusionCpp.Abstract;

namespace TahaKonsept.Agent.Engines.Image.StableDiffusionCpp.Base;

/// <summary>
/// sd-cli argüman üreticisi tabanı. Sıra: model dosyaları (rol → bayrak, <see cref="FlagFor"/>) → istem → boyut/tohum/adım →
/// örnekleme (cfg, sampler, scheduler, flow-shift) → modele özgü ek argümanlar → düşük VRAM bayrakları (<see cref="LowVramArgs"/>) → çıktı.
/// Sayılar her zaman <see cref="CultureInfo.InvariantCulture"/> ile yazılır (Türkçe kültürde ondalık virgül sorunu olmasın).
/// </summary>
public abstract class SdCppArgumentBuilderBase : ISdCppArgumentBuilder
{
    /// <inheritdoc />
    public IReadOnlyList<string> Build(ImageModelDefinition model, Func<ImageModelFile, string> resolvePath, ImageRequest request, bool lowVram)
    {
        var args = new List<string>();
        foreach (var file in model.Files)
        {
            args.Add(FlagFor(file.Role));
            args.Add(resolvePath(file));
        }

        args.AddRange(["-p", request.Prompt.Trim()]);
        if (!string.IsNullOrWhiteSpace(request.NegativePrompt))
        {
            args.AddRange(["-n", request.NegativePrompt.Trim()]);
        }

        args.AddRange(["-W", Format(request.Width), "-H", Format(request.Height)]);
        args.AddRange(["-s", request.Seed.ToString(CultureInfo.InvariantCulture)]);
        args.AddRange(["--steps", Format(request.Steps ?? model.DefaultSteps)]);
        args.AddRange(["--cfg-scale", Format(model.CfgScale)]);
        if (!string.IsNullOrWhiteSpace(model.SamplingMethod))
        {
            args.AddRange(["--sampling-method", model.SamplingMethod]);
        }

        if (!string.IsNullOrWhiteSpace(model.Scheduler))
        {
            args.AddRange(["--scheduler", model.Scheduler]);
        }

        if (model.FlowShift is { } shift)
        {
            args.AddRange(["--flow-shift", Format(shift)]);
        }

        args.AddRange(model.ExtraArgs);
        if (lowVram)
        {
            args.AddRange(LowVramArgs);
        }

        args.AddRange(["-o", request.OutputPath]);
        return args;
    }

    /// <summary>Düşük VRAM (≈6 GB) için eklenen bayraklar.</summary>
    protected abstract IReadOnlyList<string> LowVramArgs { get; }

    /// <summary>Dosya rolünün sd-cli bayrağı.</summary>
    /// <param name="role">Rol.</param>
    /// <returns>Bayrak.</returns>
    protected abstract string FlagFor(ImageModelFileRole role);

    /// <summary>Tamsayıyı kültürden bağımsız yazar.</summary>
    /// <param name="value">Değer.</param>
    /// <returns>Metin.</returns>
    protected static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Ondalığı kültürden bağımsız yazar.</summary>
    /// <param name="value">Değer.</param>
    /// <returns>Metin.</returns>
    protected static string Format(double value) => value.ToString("0.0###", CultureInfo.InvariantCulture);
}
