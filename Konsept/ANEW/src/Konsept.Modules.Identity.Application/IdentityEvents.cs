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

    /// <summary>Ajan eşleştirildi (spec 0012 Req 14; kod, sır ve belirteç içermez).</summary>
    public const string AgentPaired = "identity.agent_paired";

    /// <summary>Kullanıcı banlandı (spec 0024 Req 8; olay metninde sebep yok).</summary>
    public const string Banned = "identity.banned";

    /// <summary>Kullanıcının banı kaldırıldı.</summary>
    public const string Unbanned = "identity.unbanned";

    /// <summary>Eşleştirme olayının metni: cihaz adı ve sürümü.</summary>
    /// <param name="machineName">Makine adı.</param>
    /// <param name="agentVersion">Ajan sürümü.</param>
    /// <returns>Türkçe metin.</returns>
    public static string AgentPairedMessage(string machineName, string agentVersion) =>
        string.Format(System.Globalization.CultureInfo.CurrentCulture, IdentityText.EventAgentPaired, machineName, agentVersion);

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
        Banned => IdentityText.EventBanned,
        Unbanned => IdentityText.EventUnbanned,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Bilinmeyen olay türü."),
    };
}
