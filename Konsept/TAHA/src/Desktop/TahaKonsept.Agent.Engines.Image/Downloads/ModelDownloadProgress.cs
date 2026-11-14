namespace TahaKonsept.Agent.Engines.Image.Downloads;

/// <summary>Tek dosya indirme ilerlemesi.</summary>
/// <param name="BytesReceived">Diske yazılan toplam bayt (devam edilen kısım dahil).</param>
/// <param name="TotalBytes">Toplam boyut; bilinmiyorsa <c>null</c>.</param>
public sealed record ModelDownloadProgress(long BytesReceived, long? TotalBytes)
{
    /// <summary>Yüzde (0–100) ya da <c>null</c>.</summary>
    public double? Percent => TotalBytes is > 0 ? Math.Min(100d, BytesReceived * 100d / TotalBytes.Value) : null;
}
