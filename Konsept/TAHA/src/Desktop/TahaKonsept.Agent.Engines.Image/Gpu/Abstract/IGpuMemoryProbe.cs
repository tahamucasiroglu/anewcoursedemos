namespace TahaKonsept.Agent.Engines.Image.Gpu.Abstract;

/// <summary>
/// Anlık boş GPU belleğini (MiB) okur — görsel üretiminden hemen önce bellek tasarruf kipini seçmek için
/// (masaüstü uygulamaları 6 GB kartın ~2 GB'ını tutabilir). Önemli metot: <see cref="GetFreeVramMiBAsync"/>.
/// </summary>
public interface IGpuMemoryProbe
{
    /// <summary>İlk NVIDIA GPU'nun boş belleği.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Boş MiB; okunamazsa (NVIDIA yok, <c>nvidia-smi</c> yok/hata) <c>null</c>.</returns>
    Task<int?> GetFreeVramMiBAsync(CancellationToken ct);
}
