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

    /// <summary>Ajan hub'ının kimlik şeması (spec 0012): <c>Authorization: Bearer</c> başlığında ajan oturum belirteci.</summary>
    public const string AgentScheme = "AgentToken";

    /// <summary>Ajan hub'ı politikası: yalnızca geçerli ajan oturumu (eşleşmiş, iptal edilmemiş cihaz) bağlanabilir.</summary>
    public const string AgentHub = "agent-hub";

    /// <summary>Ajan principal'ında cihaz kimliği claim'i.</summary>
    public const string AgentDeviceClaim = "konsept.agent";

    /// <summary>Eşleştirme kod alma/sorgu/onay/ret uçlarının IP başına hız sınırı (spec 0012 Req 8; 0002 Req 9 ile aynı sınır).</summary>
    public const string AgentPairingRateLimit = "agent-pairing";

    /// <summary>Admin alanı politikası; adı ve gerektirdiği yetki <c>admin.panel</c>'dir (Identity <c>Permissions.AdminPanel</c>).</summary>
    public const string AdminPanel = "admin.panel";
}
