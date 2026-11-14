namespace Konsept.Web.ApiClient;

/// <summary>API istemcisinin ayarları.</summary>
/// <param name="ServiceKey">İç uçlar için paylaşılan servis anahtarı (yalnızca Pi'deki env dosyasından; loglanmaz).</param>
public sealed record ApiClientOptions(string? ServiceKey);
