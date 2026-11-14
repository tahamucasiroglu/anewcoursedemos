using System.Text;

namespace Konsept.Shared.Kernel.Security;

/// <summary>
/// Tarayıcının çıktısı: aralık değiştirmelerini (sırası önemsiz) biriktirir; değişiklik yoksa girdiyi aynen döner.
/// Çakışan aralıklarda önce başlayan kazanır, sonrakinin taşan kısmı da gizlenir (hiçbir parça açıkta kalmaz). Her
/// gizlenen değer <see cref="SecretValueSet"/>'e de yazılır (aynı değerin metindeki başka geçişleri için).
/// </summary>
/// <param name="text">Girdi.</param>
/// <param name="secrets">Gizlenen değerlerin kaydı (isteğe bağlı).</param>
internal sealed class SecretMaskOutput(string text, SecretValueSet? secrets = null)
{
    private readonly List<(int Start, int End, string Replacement)> _ranges = [];

    /// <summary><c>[start, end)</c> aralığını <paramref name="replacement"/> ile değiştirir.</summary>
    /// <param name="start">Başlangıç.</param>
    /// <param name="end">Bitiş (hariç).</param>
    /// <param name="replacement">Yeni metin.</param>
    public void Replace(int start, int end, string replacement)
    {
        if (end <= start)
        {
            return;
        }

        _ranges.Add((start, end, replacement));
        if (end - start <= SecretValueSet.MaxLength)
        {
            secrets?.Add(text[start..end], replacement);
        }
    }

    /// <summary>Gets or sets girintili satır maskelemesinin ulaştığı konum (aynı satırlar tekrar taranmasın).</summary>
    public int IndentedUntil { get; set; }

    /// <summary>Sonucu üretir.</summary>
    /// <returns>Maskelenmiş metin.</returns>
    public string Result()
    {
        if (_ranges.Count == 0)
        {
            return text;
        }

        _ranges.Sort((a, b) => a.Start != b.Start ? a.Start.CompareTo(b.Start) : b.End.CompareTo(a.End));
        var builder = new StringBuilder(text.Length);
        var copied = 0;
        foreach (var (start, end, replacement) in _ranges)
        {
            if (end <= copied)
            {
                continue;
            }

            if (start < copied)
            {
                // Taşan kuyruk: önceki değiştirmenin ötesi de gizlenir.
                builder.Append(SecretMasker.Hidden);
            }
            else
            {
                builder.Append(text, copied, start - copied).Append(replacement);
            }

            copied = end;
        }

        return builder.Append(text, copied, text.Length - copied).ToString();
    }
}
