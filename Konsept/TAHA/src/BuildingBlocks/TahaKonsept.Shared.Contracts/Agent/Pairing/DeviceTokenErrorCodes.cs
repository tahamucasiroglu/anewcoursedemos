namespace TahaKonsept.Shared.Contracts.Agent.Pairing;

/// <summary>
/// Cihaz token yoklamasında API'nin döndüğü hata kodları (ProblemDetails <c>code</c> alanı).
/// </summary>
public static class DeviceTokenErrorCodes
{
    /// <summary>Kullanıcı henüz onaylamadı; yoklamaya devam et.</summary>
    public const string AuthorizationPending = "agents.device.authorization_pending";

    /// <summary>Çok sık yokluyor; aralığı artır.</summary>
    public const string SlowDown = "agents.device.slow_down";

    /// <summary>Kodun süresi doldu; eşlemeyi yeniden başlat.</summary>
    public const string ExpiredToken = "agents.device.expired_token";

    /// <summary>Kullanıcı reddetti.</summary>
    public const string AccessDenied = "agents.device.access_denied";
}
