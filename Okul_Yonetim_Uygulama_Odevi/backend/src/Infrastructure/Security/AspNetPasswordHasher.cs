using Microsoft.AspNetCore.Identity;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Infrastructure.Security;

/// <summary>
/// ASP.NET Core Identity şifre özeti (PBKDF2-HMAC-SHA512, tuzlu, yüksek yineleme — docs/security.md). Özet yoksa
/// (kişi bulunamadı) sabit bir sahte özet doğrulanır: yanıt süresi "kişi var mı" bilgisini sızdırmasın.
/// </summary>
public sealed class AspNetPasswordHasher : IPasswordHasher
{
    private static readonly PasswordHasher<User> Hasher = new();
    private static readonly string DummyHash = Hasher.HashPassword(null!, Guid.NewGuid().ToString("N"));

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        return Hasher.HashPassword(null!, password);
    }

    public bool Verify(string? passwordHash, string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        PasswordVerificationResult result = Hasher.VerifyHashedPassword(null!, passwordHash ?? DummyHash, password);
        return passwordHash is not null && result != PasswordVerificationResult.Failed;
    }
}
