using System.Security.Cryptography;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Infrastructure.Security;

/// <summary>
/// Geçici şifre (spec 0005, Ö-1): 12 karakter; güvenli rastgele kaynak; harf ve rakam; birbirine benzeyen 0, O, 1, l, I
/// hiç kullanılmaz; en az bir harf ve en az bir rakam içerir. Üretilen değer hiçbir yerde saklanmaz ve loglanmaz.
/// </summary>
public sealed class TemporaryPasswordGenerator : ITemporaryPasswordGenerator
{
    public const int Length = 12;
    private const string Letters = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Digits = "23456789";
    private const string All = Letters + Digits;

    public string Generate()
    {
        while (true)
        {
            string candidate = RandomNumberGenerator.GetString(All, Length);
            if (candidate.Any(c => Digits.Contains(c, StringComparison.Ordinal)) && candidate.Any(c => Letters.Contains(c, StringComparison.Ordinal)))
            {
                return candidate;
            }
        }
    }
}
