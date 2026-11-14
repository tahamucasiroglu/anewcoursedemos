using System.Text;

namespace Konsept.Shared.Kernel.Security;

/// <summary>
/// Tarayıcının çıktısı: sırayla gelen, çakışmayan aralık değiştirmelerini biriktirir; değişiklik yoksa girdiyi aynen
/// döner. Bir aralık öncekiyle çakışırsa (geriye dönük) yok sayılır.
/// </summary>
/// <param name="text">Girdi.</param>
internal sealed class SecretMaskOutput(string text)
{
    private StringBuilder? _builder;
    private int _copied;

    /// <summary><c>[start, end)</c> aralığını <paramref name="replacement"/> ile değiştirir.</summary>
    /// <param name="start">Başlangıç.</param>
    /// <param name="end">Bitiş (hariç).</param>
    /// <param name="replacement">Yeni metin.</param>
    public void Replace(int start, int end, string replacement)
    {
        if (start < _copied || end < start)
        {
            return;
        }

        _builder ??= new StringBuilder(text.Length);
        _builder.Append(text, _copied, start - _copied).Append(replacement);
        _copied = end;
    }

    /// <summary>Sonucu üretir.</summary>
    /// <returns>Maskelenmiş metin.</returns>
    public string Result() => _builder is null ? text : _builder.Append(text, _copied, text.Length - _copied).ToString();
}
