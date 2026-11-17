using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Web.ApiClient;

/// <summary>
/// Web tarafında API çağrısının beklenen hataları ve Api'nin kararlı hata kodları/ek bilgi anahtarları (Web modüllere
/// referans vermez, Y-5; kodlar Api sözleşmesidir).
/// </summary>
public static class ApiClientErrors
{
    /// <summary>Hata kodu: API'ye ulaşılamadı.</summary>
    public const string UnavailableCode = "api.unavailable";

    /// <summary>Hata kodu: alan doğrulaması (Api <c>errors</c> döndürdü).</summary>
    public const string ValidationCode = "validation.failed";

    /// <summary>Hata kodu: e-posta zaten kayıtlı.</summary>
    public const string EmailTakenCode = "identity.email_taken";

    /// <summary>Hata kodu: e-posta ya da şifre hatalı.</summary>
    public const string InvalidCredentialsCode = "identity.invalid_credentials";

    /// <summary>Hata kodu: yanlış şifre (kalan hak ek bilgide).</summary>
    public const string WrongPasswordCode = "identity.wrong_password";

    /// <summary>Hata kodu: hesap kilitli (açılma zamanı ek bilgide).</summary>
    public const string LockedCode = "identity.locked";

    /// <summary>Hata kodu: hesap askıya alınmış (banlı; spec 0024 Req 5).</summary>
    public const string SuspendedCode = "identity.suspended";

    /// <summary>Hata kodu: çok fazla deneme.</summary>
    public const string TooManyAttemptsCode = "auth.too_many_attempts";

    /// <summary>Ek bilgi anahtarı: kalan hak.</summary>
    public const string RemainingAttemptsKey = "remainingAttempts";

    /// <summary>Ek bilgi anahtarı: kilidin açılma zamanı (UTC).</summary>
    public const string LockedUntilKey = "lockedUntil";

    /// <summary>Ek bilgi anahtarı: Api'nin korelasyon numarası.</summary>
    public const string CorrelationIdKey = "correlationId";

    /// <summary>Hata kodu: yetki yok (Api 403; spec 0011).</summary>
    public const string ForbiddenCode = "auth.forbidden";

    /// <summary>Gets API'ye ulaşılamadı.</summary>
    public static Error Unavailable { get; } = new(UnavailableCode, "API erişilemiyor", ErrorType.Unavailable);

    /// <summary>Gets yetki yok (gövdesiz 403). Kullanıcıya gösterilen metin Web'in resx'inden gelir.</summary>
    public static Error Forbidden { get; } = new(ForbiddenCode, "Yetki yok", ErrorType.Forbidden);
}
