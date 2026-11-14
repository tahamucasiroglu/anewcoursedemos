namespace TahaKonsept.BuildingBlocks.Application.Abstract;

/// <summary>
/// Gizli değer (API key) koruyucu: AES-256-GCM ile şifreler/çözer, gösterim için maskeler.
/// Veritabanında yalnızca <see cref="Protect"/> çıktısı ve <see cref="Mask"/> sonucu saklanır.
/// </summary>
public interface ISecretProtector
{
    /// <summary>Düz metni şifreler (çıktı: <c>v1:</c> önekli base64).</summary>
    /// <param name="plaintext">Düz metin.</param>
    /// <returns>Şifreli metin.</returns>
    string Protect(string plaintext);

    /// <summary>Şifreli metni çözer.</summary>
    /// <param name="protectedValue">Şifreli metin.</param>
    /// <returns>Düz metin.</returns>
    string Unprotect(string protectedValue);

    /// <summary>Şifreli metni çözmeyi dener (anahtar değişmiş/bozuk veri durumunda <c>false</c>).</summary>
    /// <param name="protectedValue">Şifreli metin.</param>
    /// <param name="plaintext">Çözülen düz metin.</param>
    /// <returns>Başarılıysa <c>true</c>.</returns>
    bool TryUnprotect(string protectedValue, out string plaintext);

    /// <summary>Gösterim maskesi üretir (son 4 karakter açık, ör. <c>••••abcd</c>).</summary>
    /// <param name="plaintext">Düz metin.</param>
    /// <returns>Maske.</returns>
    string Mask(string plaintext);
}
