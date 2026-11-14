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
}
