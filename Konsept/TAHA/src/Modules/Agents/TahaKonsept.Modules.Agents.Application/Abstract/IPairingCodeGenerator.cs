namespace TahaKonsept.Modules.Agents.Application.Abstract;

/// <summary>
/// Eşleme kodları: gizli cihaz kodu (32 bayt, base64url), 8 karakterlik kullanıcı kodu (karışan harfler yok),
/// kodların normalize/biçimlendirme işlemleri ve cihaz kodu hash'i (SHA256 hex).
/// </summary>
public interface IPairingCodeGenerator
{
    /// <summary>Yeni gizli cihaz kodu.</summary>
    /// <returns>Cihaz kodu.</returns>
    string CreateDeviceCode();

    /// <summary>Yeni kullanıcı kodu (tiresiz, 8 karakter, büyük harf).</summary>
    /// <returns>Kod.</returns>
    string CreateUserCode();

    /// <summary>Cihaz kodunun hash'i.</summary>
    /// <param name="deviceCode">Cihaz kodu.</param>
    /// <returns>SHA256 hex.</returns>
    string HashDeviceCode(string deviceCode);

    /// <summary>Kullanıcının yazdığı kodu normalize eder (tire/boşluk atılır, büyük harf).</summary>
    /// <param name="userCode">Kod.</param>
    /// <returns>Normalize kod.</returns>
    string NormalizeUserCode(string userCode);

    /// <summary>Kodu gösterim biçimine çevirir (<c>XXXX-XXXX</c>).</summary>
    /// <param name="normalizedUserCode">Normalize kod.</param>
    /// <returns>Biçimli kod.</returns>
    string FormatUserCode(string normalizedUserCode);
}
