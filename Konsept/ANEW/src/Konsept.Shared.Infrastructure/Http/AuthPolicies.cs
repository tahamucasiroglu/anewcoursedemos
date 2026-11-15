namespace Konsept.Shared.Infrastructure.Http;

/// <summary>
/// Api'nin kimlik şemaları ve politika adları (plan 0002): kullanıcı uçları JWT ister (default-deny), iç uçlar servis
/// anahtarı ister, giriş/kayıt IP başına hız sınırına tabidir.
/// </summary>
public static class AuthPolicies
{
    /// <summary>Servis anahtarı kimlik şemasının adı.</summary>
    public const string ServiceKeyScheme = "ServiceKey";

    /// <summary>Yalnızca Web'in servis anahtarıyla erişebildiği iç uç politikası (ör. <c>/api/internal/logs</c>).</summary>
    public const string InternalService = "internal-service";

    /// <summary>Giriş ve kayıt uçlarının IP başına hız sınırı politikası (spec 0002 Req 9).</summary>
    public const string AuthRateLimit = "auth";

    /// <summary>
    /// Yetki claim'inin adı (spec 0011). Token'da taşınmaz: Api her istekte oturumu doğrularken rolü veritabanından okur ve
    /// bu claim'leri principal'a ekler; token'dan gelen aynı adlı claim'ler önce silinir.
    /// </summary>
    public const string PermissionClaim = "konsept.perm";

    /// <summary>Admin alanı politikası; adı ve gerektirdiği yetki <c>admin.panel</c>'dir (Identity <c>Permissions.AdminPanel</c>).</summary>
    public const string AdminPanel = "admin.panel";
}
