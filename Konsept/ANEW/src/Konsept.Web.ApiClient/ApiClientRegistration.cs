using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.ApiClient.Concrete;
using Microsoft.Extensions.DependencyInjection;

namespace Konsept.Web.ApiClient;

/// <summary>
/// API istemcisinin servis kaydı.
/// </summary>
public static class ApiClientRegistration
{
    /// <summary>"Ajanı test et" 10 sn bekleyebildiği için istek süresi bundan uzun tutulur.</summary>
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

    /// <summary>API istemcisini kaydeder.</summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <param name="baseUrl">API'nin taban adresi (ör. <c>http://localhost:8001/</c>).</param>
    /// <param name="serviceKey">İç uçlar için paylaşılan servis anahtarı (yoksa iç uçlar 401 döner).</param>
    /// <returns>Aynı koleksiyon.</returns>
    public static IServiceCollection AddKonseptApiClient(this IServiceCollection services, Uri baseUrl, string? serviceKey = null)
    {
        services.AddSingleton(new ApiClientOptions(serviceKey));
        services.AddHttpClient<IKonseptApiClient, KonseptApiClient>(client =>
        {
            client.BaseAddress = baseUrl;
            client.Timeout = RequestTimeout;
        });
        return services;
    }
}
