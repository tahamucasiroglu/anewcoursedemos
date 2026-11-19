using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;

namespace OkulYonetim.Tests.Integration;

/// <summary>Tarayıcı erişim izni testleri (spec 0001 AC-15): yalnızca ayardaki web adresi izin alır.</summary>
[Trait("Category", "Integration")]
public sealed class CorsTests
{
    private readonly TestDatabase database;

    public CorsTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task AllowedOrigin_GetsCorsHeader_OtherOrigin_DoesNot()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage allowed = await SendWithOriginAsync(client, "http://localhost:5173");
        using HttpResponseMessage other = await SendWithOriginAsync(client, "http://kotu-site.example");

        allowed.Headers.GetValues("Access-Control-Allow-Origin").Single().ShouldBe("http://localhost:5173");
        other.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }

    private static async Task<HttpResponseMessage> SendWithOriginAsync(HttpClient client, string origin)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, new Uri("/api/system-health", UriKind.Relative));
        request.Headers.Add("Origin", origin);
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
