using TahaKonsept.BuildingBlocks.Application.Abstract;

namespace TahaKonsept.Modules.Agents.Tests.Support;

/// <summary>
/// Sahte gizli değer koruyucu: <c>enc:</c> öneki ekler/kaldırır.
/// </summary>
public sealed class FakeSecretProtector : ISecretProtector
{
    /// <summary>Şifreler.</summary>
    /// <param name="plaintext">Düz metin.</param>
    /// <returns>Şifreli.</returns>
    public string Protect(string plaintext) => "enc:" + plaintext;

    /// <summary>Çözer.</summary>
    /// <param name="protectedValue">Şifreli.</param>
    /// <returns>Düz metin.</returns>
    public string Unprotect(string protectedValue) => protectedValue["enc:".Length..];

    /// <summary>Çözmeyi dener.</summary>
    /// <param name="protectedValue">Şifreli.</param>
    /// <param name="plaintext">Düz metin.</param>
    /// <returns>Başarılıysa <c>true</c>.</returns>
    public bool TryUnprotect(string protectedValue, out string plaintext)
    {
        plaintext = protectedValue.StartsWith("enc:", StringComparison.Ordinal) ? protectedValue[4..] : string.Empty;
        return plaintext.Length > 0;
    }

    /// <summary>Maskeler.</summary>
    /// <param name="plaintext">Düz metin.</param>
    /// <returns>Maske.</returns>
    public string Mask(string plaintext) => "••••";
}
