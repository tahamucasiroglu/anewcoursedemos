using System.Globalization;
using TahaKonsept.Agent.Engines.Image.Gpu.Abstract;

namespace TahaKonsept.Agent.Engines.Image.Gpu.Base;

/// <summary>
/// <see cref="IGpuMemoryProbe"/> ortak davranışı: somut sınıfın döndürdüğü <c>nvidia-smi --query-gpu=memory.free --format=csv,noheader,nounits</c>
/// çıktısının ilk satırını (<see cref="Parse"/>) MiB'e çevirir; sorgu hatası/zaman aşımı <c>null</c> olur (üretimi asla bozmaz).
/// </summary>
public abstract class GpuMemoryProbeBase : IGpuMemoryProbe
{
    /// <inheritdoc />
    public async Task<int?> GetFreeVramMiBAsync(CancellationToken ct)
    {
        try
        {
            var output = await QueryAsync(ct).ConfigureAwait(false);
            return output is null ? null : Parse(output);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    /// <summary>Sorgu çıktısını çözer (ilk satır, tamsayı MiB).</summary>
    /// <param name="output">Çıktı.</param>
    /// <returns>MiB ya da <c>null</c>.</returns>
    public static int? Parse(string? output)
    {
        var first = (output ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        return int.TryParse(first, NumberStyles.Integer, CultureInfo.InvariantCulture, out var mib) && mib >= 0 ? mib : null;
    }

    /// <summary>Ham sorgu çıktısı (başarısızsa <c>null</c>).</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Çıktı.</returns>
    protected abstract Task<string?> QueryAsync(CancellationToken ct);
}
