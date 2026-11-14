namespace Konsept.Shared.Kernel.Security;

/// <summary>
/// Web ↔ Api iç uçlarının paylaşılan servis anahtarı başlığı (plan 0008, Risk 2). Anahtarın değeri yalnızca Pi'deki
/// <c>/etc/anewconcept/*.env</c> dosyalarında (<c>Internal__ServiceKey</c>) durur; loglanmaz.
/// </summary>
public static class ServiceKeyHeader
{
    /// <summary>Başlık adı.</summary>
    public const string Name = "X-Konsept-Service-Key";

    /// <summary>Yapılandırma anahtarı.</summary>
    public const string ConfigurationKey = "Internal:ServiceKey";
}
