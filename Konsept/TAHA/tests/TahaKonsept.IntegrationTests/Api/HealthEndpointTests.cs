using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Xunit;
using TahaKonsept.IntegrationTests.Support;
using TahaKonsept.Shared.Contracts.Common;

namespace TahaKonsept.IntegrationTests.Api;

/// <summary>
/// <c>GET /api/health</c> testleri: anonim erişilebilir, sürüm ve veritabanı durumunu döner.
/// </summary>
public sealed class HealthEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    /// <summary>Fabrikayı alır.</summary>
    /// <param name="factory">API fabrikası.</param>
    public HealthEndpointTests(ApiFactory factory) => _factory = factory;

    /// <summary>Sağlık ucu oturum açmadan 200, sürüm ve veritabanı durumu döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Health_IsAnonymous_AndReturnsVersion()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/health", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>(TestContext.Current.CancellationToken);
        body.ShouldNotBeNull();
        body.Version.ShouldNotBeNullOrWhiteSpace();
        body.Status.ShouldBeOneOf("ok", "degraded");
        body.Database.ShouldBeOneOf("ready", "unavailable");
    }
}
