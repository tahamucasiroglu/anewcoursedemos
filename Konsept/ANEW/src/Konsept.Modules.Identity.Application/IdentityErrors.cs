using System.Globalization;
using Konsept.Modules.Identity.Application.Resources;
using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Identity.Application;

/// <summary>
/// Identity'nin beklenen hataları (kararlı kod + Türkçe mesaj + gizli olmayan ek bilgi). Web kodlara göre kendi metnini
/// seçer (kalan hak, kilidin İstanbul saatiyle açılışı).
/// </summary>
public static class IdentityErrors
{
    /// <summary>Hata kodu: e-posta zaten kayıtlı.</summary>
    public const string EmailTakenCode = "identity.email_taken";

    /// <summary>Hata kodu: e-posta ya da şifre hatalı (kayıtlı olmayan e-posta).</summary>
    public const string InvalidCredentialsCode = "identity.invalid_credentials";

    /// <summary>Hata kodu: yanlış şifre (kalan hak <see cref="RemainingAttemptsKey"/>'de).</summary>
    public const string WrongPasswordCode = "identity.wrong_password";

    /// <summary>Hata kodu: hesap kilitli (açılma zamanı <see cref="LockedUntilKey"/>'de).</summary>
    public const string LockedCode = "identity.locked";

    /// <summary>Hata kodu: oturum ya da refresh token geçersiz.</summary>
    public const string SessionInvalidCode = "identity.session_invalid";

    /// <summary>Hata kodu: IP başına hız sınırı aşıldı.</summary>
    public const string TooManyAttemptsCode = "auth.too_many_attempts";

    /// <summary>Ek bilgi anahtarı: kalan hak.</summary>
    public const string RemainingAttemptsKey = "remainingAttempts";

    /// <summary>Ek bilgi anahtarı: kilidin açılma zamanı (UTC, ISO 8601).</summary>
    public const string LockedUntilKey = "lockedUntil";

    /// <summary>Gets e-posta zaten kayıtlı.</summary>
    public static Error EmailTaken => new(EmailTakenCode, IdentityText.EmailTaken, ErrorType.Conflict);

    /// <summary>Gets e-posta ya da şifre hatalı (kalan hak gösterilmez).</summary>
    public static Error InvalidCredentials => new(InvalidCredentialsCode, IdentityText.InvalidCredentials, ErrorType.Unauthorized);

    /// <summary>Gets oturum geçersiz.</summary>
    public static Error SessionInvalid => new(SessionInvalidCode, IdentityText.SessionInvalid, ErrorType.Unauthorized);

    /// <summary>Gets çok fazla deneme.</summary>
    public static Error TooManyAttempts => new(TooManyAttemptsCode, IdentityText.TooManyAttempts, ErrorType.TooManyRequests);

    /// <summary>Yanlış şifre hatası.</summary>
    /// <param name="remainingAttempts">Kalan hak.</param>
    /// <returns>Hata.</returns>
    public static Error WrongPassword(int remainingAttempts) =>
        new(WrongPasswordCode, string.Format(CultureInfo.CurrentCulture, IdentityText.WrongPassword, remainingAttempts), ErrorType.Unauthorized)
        {
            Metadata = new Dictionary<string, object>(StringComparer.Ordinal) { [RemainingAttemptsKey] = remainingAttempts },
        };

    /// <summary>Hesap kilitli hatası.</summary>
    /// <param name="lockedUntil">Kilidin açılma zamanı.</param>
    /// <returns>Hata.</returns>
    public static Error Locked(DateTimeOffset lockedUntil) =>
        new(LockedCode, IdentityText.AccountLocked, ErrorType.Unauthorized)
        {
            Metadata = new Dictionary<string, object>(StringComparer.Ordinal) { [LockedUntilKey] = lockedUntil.ToUniversalTime() },
        };
}
