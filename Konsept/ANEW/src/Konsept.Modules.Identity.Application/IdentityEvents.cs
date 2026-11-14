using Konsept.Modules.Identity.Application.Resources;

namespace Konsept.Modules.Identity.Application;

/// <summary>
/// Identity'nin Günlük olayları (spec 0002 Req 17): tür kodları ve Türkçe metinleri. Metinler şifre, token ya da e-posta
/// içermez; olay kullanıcıya bağlı yazılır.
/// </summary>
public static class IdentityEvents
{
    /// <summary>Kayıt olundu.</summary>
    public const string Registered = "identity.registered";

    /// <summary>Başarılı giriş.</summary>
    public const string LoginSucceeded = "identity.login_succeeded";

    /// <summary>Kayıtlı hesaba başarısız giriş.</summary>
    public const string LoginFailed = "identity.login_failed";

    /// <summary>Hesap kilitlendi.</summary>
    public const string Locked = "identity.locked";

    /// <summary>Çıkış (tüm oturumlar).</summary>
    public const string LoggedOut = "identity.logged_out";

    /// <summary>Olay türünün metni.</summary>
    /// <param name="type">Olay türü.</param>
    /// <returns>Türkçe metin.</returns>
    public static string MessageOf(string type) => type switch
    {
        Registered => IdentityText.EventRegistered,
        LoginSucceeded => IdentityText.EventLoginSucceeded,
        LoginFailed => IdentityText.EventLoginFailed,
        Locked => IdentityText.EventLocked,
        LoggedOut => IdentityText.EventLoggedOut,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Bilinmeyen olay türü."),
    };
}
