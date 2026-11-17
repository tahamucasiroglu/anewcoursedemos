using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;

namespace Konsept.Agent.Infrastructure.Base;

/// <summary>
/// Ekran kartı tespiti (spec 0028 Req 5): bağdaştırıcılardan ayrılmış belleği en büyük olan seçilir; üretici adı NVIDIA/AMD/Intel olarak
/// sınıflanır. Okuma hatası tespit edilemedi sayılır (<c>null</c>); istisna dışarı çıkmaz. Bağdaştırıcıları okumak alt sınıftadır.
/// </summary>
public abstract class GpuProbeBase : IGpuProbe
{
    /// <inheritdoc />
    public GpuInfo? Detect()
    {
        try
        {
            return ReadAdapters()
                .Where(a => a.VramBytes > 0 && !string.IsNullOrWhiteSpace(a.Name))
                .OrderByDescending(a => a.VramBytes)
                .Select(a => new GpuInfo(a.Name.Trim(), Vendor(a.Name + " " + a.Provider), a.VramBytes))
                .FirstOrDefault();
        }
#pragma warning disable CA1031 // Tespit hatası "kart yok" sayılır; ajan açılışı düşmez.
        catch (Exception)
#pragma warning restore CA1031
        {
            return null;
        }
    }

    /// <summary>Üretici sınıfı.</summary>
    /// <param name="text">Ad + sağlayıcı.</param>
    /// <returns><c>nvidia</c> · <c>amd</c> · <c>intel</c> · <c>other</c>.</returns>
    public static string Vendor(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Contains("nvidia", StringComparison.OrdinalIgnoreCase) ? GpuInfo.Nvidia
            : text.Contains("radeon", StringComparison.OrdinalIgnoreCase) || text.Contains("amd", StringComparison.OrdinalIgnoreCase) || text.Contains("advanced micro", StringComparison.OrdinalIgnoreCase) ? "amd"
            : text.Contains("intel", StringComparison.OrdinalIgnoreCase) ? "intel"
            : "other";
    }

    /// <summary>Ekran bağdaştırıcılarını okur.</summary>
    /// <returns>Ad, sağlayıcı, ayrılmış bellek (bayt).</returns>
    protected abstract IEnumerable<(string Name, string Provider, long VramBytes)> ReadAdapters();
}
