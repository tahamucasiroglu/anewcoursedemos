using System.Globalization;
using System.Security.Cryptography;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Infrastructure.Security;

/// <summary>
/// RFC 6238 zamana bağlı kod (spec 0007): HMAC-SHA1, 6 hane, 30 sn dilim; saat kayması için önceki ve sonraki dilim de
/// kabul edilir. Anahtar 20 rastgele bayt, Base32 (RFC 4648) — doğrulayıcı uygulamalarla uyumlu. Paket kullanılmaz.
/// </summary>
public sealed class Totp : ITotp
{
    public const int Digits = 6;
    public static readonly TimeSpan Period = TimeSpan.FromSeconds(30);
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public string GenerateSecret() => ToBase32(RandomNumberGenerator.GetBytes(20));

    public bool Verify(string secret, string code, DateTimeOffset now, out long matchedStep)
    {
        matchedStep = 0;
        string digits = (code ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal);
        if (digits.Length != Digits || !digits.All(char.IsAsciiDigit))
        {
            return false;
        }

        byte[] key = FromBase32(secret);
        long current = now.ToUnixTimeSeconds() / (long)Period.TotalSeconds;
        for (long candidate = current - 1; candidate <= current + 1; candidate++)
        {
            if (CryptographicOperations.FixedTimeEquals(System.Text.Encoding.ASCII.GetBytes(Code(key, candidate)), System.Text.Encoding.ASCII.GetBytes(digits)))
            {
                matchedStep = candidate;
                return true;
            }
        }

        return false;
    }

    public string ProvisioningUri(string secret, string account) =>
        $"otpauth://totp/{Uri.EscapeDataString("Okul Yönetim")}:{Uri.EscapeDataString(account)}?secret={secret}&issuer={Uri.EscapeDataString("Okul Yönetim")}&algorithm=SHA1&digits={Digits}&period={(int)Period.TotalSeconds}";

    /// <summary>Verilen dilimin kodu (RFC 4226 dinamik kesme).</summary>
    public static string Code(byte[] key, long step)
    {
        ArgumentNullException.ThrowIfNull(key);
        byte[] counter = BitConverter.GetBytes(step);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counter);
        }

        // RFC 6238/4226 HMAC-SHA1 ister; doğrulayıcı uygulamalar yalnızca bunu destekler. HMAC içindeki SHA1 çakışma
        // zayıflığından etkilenmez (özet olarak kullanılmıyor).
#pragma warning disable CA5350
        byte[] hash = HMACSHA1.HashData(key, counter);
#pragma warning restore CA5350
        int offset = hash[^1] & 0x0F;
        int binary = ((hash[offset] & 0x7F) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (binary % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }

    public static string ToBase32(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        System.Text.StringBuilder output = new();
        int buffer = 0;
        int bits = 0;
        foreach (byte value in data)
        {
            buffer = (buffer << 8) | value;
            bits += 8;
            while (bits >= 5)
            {
                output.Append(Alphabet[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }

        if (bits > 0)
        {
            output.Append(Alphabet[(buffer << (5 - bits)) & 31]);
        }

        return output.ToString();
    }

    public static byte[] FromBase32(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        List<byte> output = [];
        int buffer = 0;
        int bits = 0;
        foreach (char c in text.TrimEnd('=').ToUpperInvariant())
        {
            int value = Alphabet.IndexOf(c, StringComparison.Ordinal);
            if (value < 0)
            {
                continue;
            }

            buffer = (buffer << 5) | value;
            bits += 5;
            if (bits >= 8)
            {
                output.Add((byte)((buffer >> (bits - 8)) & 0xFF));
                bits -= 8;
            }
        }

        return [.. output];
    }
}
