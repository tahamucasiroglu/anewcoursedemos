namespace Konsept.Web.Auth;

/// <summary>
/// Web'in yetki adları ve politikaları (spec 0011). Web modüllere referans vermez (Y-5); adlar Api sözleşmesidir. Yetki
/// claim'leri cookie'den değil, her istekte Api'nin oturum yanıtından kurulur (<see cref="KonseptCookieEvents"/>); Web
/// politikası yalnızca sayfayı ve bağlantıyı gizlemek içindir, asıl karar Api'dedir.
/// </summary>
public static class WebPolicies
{
    /// <summary>Principal'daki yetki claim'inin adı.</summary>
    public const string PermissionClaim = "konsept.perm";

    /// <summary>Admin alanı yetkisi ve politika adı.</summary>
    public const string AdminPanel = "admin.panel";

    /// <summary>Yetkisiz sayfa yolu (403 durum kodlu, "Bu sayfaya erişim yetkiniz yok").</summary>
    public const string ForbiddenPath = "/yetkisiz";

    /// <summary>Ajax isteğine dönen yetki hatası kodu.</summary>
    public const string ForbiddenCode = "auth.forbidden";
}
