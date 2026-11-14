using System.Security.Cryptography;
using System.Text;
using TahaKonsept.Modules.Agents.Application.Abstract;

namespace TahaKonsept.Modules.Agents.Application.Base;

/// <summary>
/// Eşleme kodu üretici tabanı: kod alfabesi (<see cref="UserCodeAlphabet"/>; 0/O, 1/I gibi karışan karakterler yok),
/// normalize/biçimlendirme, SHA256 hash ve base64url cihaz kodu. Rastgelelik kaynağı (<see cref="FillRandom"/>) türetilen sınıftadır.
/// </summary>
public abstract class PairingCodeGeneratorBase : IPairingCodeGenerator
{
    /// <summary>Kullanıcı kodu alfabesi.</summary>
    public const string UserCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>Kullanıcı kodu uzunluğu (tiresiz).</summary>
    public const int UserCodeLength = 8;

    /// <summary>Cihaz kodu bayt uzunluğu.</summary>
    public const int DeviceCodeBytes = 32;

    /// <summary>Yeni cihaz kodu (base64url, dolgusuz).</summary>
    /// <returns>Cihaz kodu.</returns>
    public string CreateDeviceCode()
    {
        Span<byte> buffer = stackalloc byte[DeviceCodeBytes];
        FillRandom(buffer);
        return Convert.ToBase64String(buffer).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>Yeni kullanıcı kodu.</summary>
    /// <returns>Kod (8 karakter).</returns>
    public string CreateUserCode()
    {
        Span<byte> buffer = stackalloc byte[UserCodeLength];
        FillRandom(buffer);
        var chars = new char[UserCodeLength];
        for (var i = 0; i < UserCodeLength; i++)
        {
            // Alfabe 32 karakter: 256 % 32 == 0 olduğu için modulo yanlılık üretmez.
            chars[i] = UserCodeAlphabet[buffer[i] % UserCodeAlphabet.Length];
        }

        return new string(chars);
    }

    /// <summary>Cihaz kodu hash'i (SHA256, küçük harf hex).</summary>
    /// <param name="deviceCode">Cihaz kodu.</param>
    /// <returns>Hash.</returns>
    public string HashDeviceCode(string deviceCode)
    {
        ArgumentNullException.ThrowIfNull(deviceCode);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(deviceCode.Trim())));
    }

    /// <summary>Kodu normalize eder.</summary>
    /// <param name="userCode">Kod.</param>
    /// <returns>Tiresiz büyük harf kod.</returns>
    public string NormalizeUserCode(string userCode)
    {
        if (string.IsNullOrWhiteSpace(userCode))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(userCode.Length);
        foreach (var ch in userCode)
        {
            if (char.IsAsciiLetterOrDigit(ch))
            {
                builder.Append(char.ToUpperInvariant(ch));
            }
        }

        return builder.ToString();
    }

    /// <summary>Kodu <c>XXXX-XXXX</c> biçimine çevirir.</summary>
    /// <param name="normalizedUserCode">Normalize kod.</param>
    /// <returns>Biçimli kod.</returns>
    public string FormatUserCode(string normalizedUserCode)
    {
        ArgumentNullException.ThrowIfNull(normalizedUserCode);
        return normalizedUserCode.Length == UserCodeLength
            ? string.Concat(normalizedUserCode.AsSpan(0, 4), "-", normalizedUserCode.AsSpan(4))
            : normalizedUserCode;
    }

    /// <summary>Tamponu kriptografik rastgele baytlarla doldurur.</summary>
    /// <param name="buffer">Tampon.</param>
    protected abstract void FillRandom(Span<byte> buffer);
}
