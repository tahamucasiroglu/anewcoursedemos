using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;

namespace OkulYonetim.Tests.Integration;

/// <summary>Sistem durumu ucunun gerçek PostgreSQL ile testleri (spec 0001 AC-1, AC-2).</summary>
[Trait("Category", "Integration")]
public sealed class SystemHealthEndpointTests
{
    private const string UnreachableDatabase = "Host=127.0.0.1;Port=1;Database=yok;Username=yok;Password=yok;Timeout=2";
    private readonly TestDatabase database;

    public SystemHealthEndpointTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Get_DatabaseUp_ReturnsHealthyWithTurkishMessage()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(new Uri("/api/system-health", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonElement body = await ReadBodyAsync(response);
        body.GetProperty("status").GetString().ShouldBe("healthy");
        body.GetProperty("message").GetString().ShouldBe("Sistem çalışıyor");
        PropertyNames(body).ShouldBe(["message", "status"], ignoreOrder: true);
    }

    [Fact]
    public async Task Get_DatabaseUnreachable_Returns503WithoutDetails()
    {
        using OkulApiFactory factory = new(UnreachableDatabase);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(new Uri("/api/system-health", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        JsonElement body = await ReadBodyAsync(response);
        body.GetProperty("status").GetString().ShouldBe("unavailable");
        body.GetProperty("message").GetString().ShouldBe("Sistem şu anda hizmet dışı");
        PropertyNames(body).ShouldBe(["message", "status"], ignoreOrder: true);
        string raw = body.GetRawText();
        raw.ShouldNotContain("127.0.0.1");
        raw.ShouldNotContain("Npgsql", Case.Insensitive);
        raw.ShouldNotContain("exception", Case.Insensitive);
    }

    private static async Task<JsonElement> ReadBodyAsync(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    private static string[] PropertyNames(JsonElement body) =>
        body.EnumerateObject().Select(p => p.Name).ToArray();
}
