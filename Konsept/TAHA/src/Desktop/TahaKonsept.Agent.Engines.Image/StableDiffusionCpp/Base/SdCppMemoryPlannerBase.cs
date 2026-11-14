using System.Globalization;
using TahaKonsept.Agent.Engines.Image.Catalog;
using TahaKonsept.Agent.Engines.Image.StableDiffusionCpp.Abstract;

namespace TahaKonsept.Agent.Engines.Image.StableDiffusionCpp.Base;

/// <summary>
/// <see cref="ISdCppMemoryPlanner"/> kuralları (MiB; D = difüzyon/tam model dosyası boyutu):
/// <list type="bullet">
/// <item><c>aggressive</c> → <see cref="SdCppMemoryLevel.Minimal"/>: metin kodlayıcı + VAE CPU'da, VAE döşeme, flash attention,
/// <c>--max-vram</c> = (boş − <see cref="MinimalReserveMiB"/>) GiB (en az <see cref="MinBudgetGiB"/>; boş bilinmiyorsa <see cref="UnknownMinimalBudgetGiB"/>).</item>
/// <item>boş bilinmiyor ya da boş ≥ D + <see cref="StandardHeadroomMiB"/> → <see cref="SdCppMemoryLevel.Standard"/> (<see cref="StandardArgs"/>, kapalıysa hiç).</item>
/// <item>aksi halde → <see cref="SdCppMemoryLevel.Reduced"/>: metin kodlayıcı CPU'da, VAE döşeme, flash attention,
/// <c>--max-vram</c> = (boş − <see cref="ReducedReserveMiB"/>) GiB — sd.cpp ağırlıkların sığmayanını RAM'de tutup katman katman çalıştırır.</item>
/// </list>
/// Ölçüm (RTX 3060 Laptop 6 GB, Z-Image-Turbo Q3_K 512x768, 3,65 GB boş): Standard → CUDA OOM; Reduced (<c>--max-vram 2.5</c>) → 24 sn'de görsel.
/// </summary>
public abstract class SdCppMemoryPlannerBase : ISdCppMemoryPlanner
{
    /// <summary>Standart kip için difüzyon modeli üstüne gereken boş VRAM payı (metin kodlayıcı geçici yükü + hesap tamponları).</summary>
    public const int StandardHeadroomMiB = 2048;

    /// <summary>Azaltılmış kipte bütçe dışında bırakılan pay (CUDA bağlamı, VAE döşeme, parçalanma).</summary>
    public const int ReducedReserveMiB = 1152;

    /// <summary>En agresif kipte bırakılan pay.</summary>
    public const int MinimalReserveMiB = 1536;

    /// <summary>En düşük <c>--max-vram</c> bütçesi (GiB).</summary>
    public const double MinBudgetGiB = 0.5;

    /// <summary>Boş VRAM bilinmiyorken en agresif kip bütçesi (GiB).</summary>
    public const double UnknownMinimalBudgetGiB = 1.5;

    /// <summary>Model boyutu bilinmiyorsa (ya da 1 MiB altındaysa) varsayılan difüzyon boyutu.</summary>
    public const int UnknownDiffusionMiB = 3072;

    /// <inheritdoc />
    public SdCppMemoryPlan Plan(ImageModelDefinition model, int? freeVramMiB, bool lowVramDefault, bool aggressive)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (aggressive)
        {
            var budget = freeVramMiB is { } f ? Budget(f, MinimalReserveMiB) : UnknownMinimalBudgetGiB;
            return new SdCppMemoryPlan(SdCppMemoryLevel.Minimal, MinimalArgs(budget), freeVramMiB);
        }

        if (freeVramMiB is not { } free || free >= DiffusionMiB(model) + StandardHeadroomMiB)
        {
            return new SdCppMemoryPlan(SdCppMemoryLevel.Standard, lowVramDefault ? StandardArgs : [], freeVramMiB);
        }

        return new SdCppMemoryPlan(SdCppMemoryLevel.Reduced, ReducedArgs(Budget(free, ReducedReserveMiB)), freeVramMiB);
    }

    /// <summary>Difüzyon (ya da tam) model dosyalarının toplam boyutu (MiB).</summary>
    /// <param name="model">Model.</param>
    /// <returns>MiB.</returns>
    public static int DiffusionMiB(ImageModelDefinition model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var bytes = model.Files
            .Where(f => f.Role is ImageModelFileRole.DiffusionModel or ImageModelFileRole.FullModel)
            .Sum(f => f.SizeBytes);
        var mib = (int)(bytes / (1024 * 1024));
        return mib > 0 ? mib : UnknownDiffusionMiB;
    }

    /// <summary>Bütçeyi GiB metnine çevirir (kültürden bağımsız, en fazla 2 ondalık).</summary>
    /// <param name="gib">GiB.</param>
    /// <returns>Metin.</returns>
    protected static string FormatGiB(double gib) => gib.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>Standart düşük VRAM bayrakları.</summary>
    protected abstract IReadOnlyList<string> StandardArgs { get; }

    /// <summary>Azaltılmış kip bayrakları.</summary>
    /// <param name="budgetGiB">GPU bütçesi (GiB).</param>
    /// <returns>Bayraklar.</returns>
    protected abstract IReadOnlyList<string> ReducedArgs(double budgetGiB);

    /// <summary>En agresif kip bayrakları.</summary>
    /// <param name="budgetGiB">GPU bütçesi (GiB).</param>
    /// <returns>Bayraklar.</returns>
    protected abstract IReadOnlyList<string> MinimalArgs(double budgetGiB);

    private static double Budget(int freeMiB, int reserveMiB) =>
        Math.Max(MinBudgetGiB, Math.Floor((freeMiB - reserveMiB) / 1024.0 * 100) / 100);
}
