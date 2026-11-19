namespace OkulYonetim.Application.Features.Identity;

/// <summary>
/// Şifre kuralı (spec 0004, gereksinim 8): en az <see cref="MinimumLength"/> (8) karakter; MVP'de başka kural yok
/// (çok bilinen şifre listesi pilot öncesine ertelendi — backlog B-17).
/// </summary>
public static class PasswordPolicy
{
    public const int MinimumLength = 8;

    public static bool IsSatisfiedBy(string? password) => password is { Length: >= MinimumLength };
}
