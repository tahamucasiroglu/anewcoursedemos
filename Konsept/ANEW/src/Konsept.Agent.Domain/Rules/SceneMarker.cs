using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Konsept.Agent.Domain.Rules;

/// <summary>
/// Tamamlanmış sahne render'ının işareti (spec 0018 Req 5, AC-7): klip çıktı kapısından geçince sahne klasörüne <see cref="FileName"/> yazılır;
/// içeriği kod + biçim + süre + klip adının SHA-256'sıdır. Devam ya da düzeltmede işareti tutan sahne yeniden render edilmez; kod değişirse
/// (düzeltme) işaret tutmaz.
/// </summary>
public static class SceneMarker
{
    /// <summary>İşaret dosyasının adı.</summary>
    public const string FileName = "clip.ok";

    /// <summary>İşaret değeri.</summary>
    /// <param name="code">Sahne kodu.</param>
    /// <param name="format">Biçim.</param>
    /// <param name="durationSeconds">Süre.</param>
    /// <param name="clipName">Klip adı.</param>
    /// <returns>Küçük harf onaltılık SHA-256.</returns>
    public static string Of(string code, string format, int durationSeconds, string clipName)
    {
        string[] parts = [code, format, durationSeconds.ToString(CultureInfo.InvariantCulture), clipName];
        var text = string.Join("\n", parts);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }
}
