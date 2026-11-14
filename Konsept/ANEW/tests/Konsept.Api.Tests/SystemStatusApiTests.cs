using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Konsept.Api.Tests.Support;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Sistem durumu ve sağlık kontrolü (spec 0001 AC-4).
/// </summary>
public sealed class SystemStatusApiTests : IClassFixture<FastApiFactory>
{
    private readonly FastApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="SystemStatusApiTests"/> class.</summary>
    /// <param name="factory">Hızlı API fabrikası.</param>
    public SystemStatusApiTests(FastApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Veritabanı erişilemezken durum 200 döner ama "veritabanı yok" der; sağlık "down" der.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task SystemStatus_ApiOrDbDown_ReturnsErrorState()
    {
        _factory.Database.Available = false;
        try
        {
            using var client = _factory.CreateClient();

            var status = await client.GetFromJsonAsync<JsonElement>("/api/system/status", TestContext.Current.CancellationToken);
            var health = await client.GetFromJsonAsync<JsonElement>("/health", TestContext.Current.CancellationToken);

            status.GetProperty("databaseAvailable").GetBoolean().ShouldBeFalse();
            status.GetProperty("lastEvent").ValueKind.ShouldBe(JsonValueKind.Null);
            health.GetProperty("database").GetString().ShouldBe("down");
            health.GetProperty("api").GetString().ShouldBe("ok");
        }
        finally
        {
            _factory.Database.Available = true;
        }
    }

    /// <summary>Sürüm sabit metin değildir; derlemenin versiyon bilgisinden okunur ve tek ürün versiyonuna eşittir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Version_ReadFromAssembly_NotLiteral()
    {
        using var client = _factory.CreateClient();
        var expected = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion.Split('+')[0];

        var status = await client.GetFromJsonAsync<JsonElement>("/api/system/status", TestContext.Current.CancellationToken);
        var health = await client.GetFromJsonAsync<JsonElement>("/health", TestContext.Current.CancellationToken);

        status.GetProperty("version").GetString().ShouldBe(expected);
        health.GetProperty("version").GetString().ShouldBe(expected);
        expected.ShouldBe(typeof(Program).Assembly.GetName().Version!.ToString(3));
    }

    /// <summary>Sağlık kontrolü ajan ya da kullanıcı bilgisi içermez (docs/architecture.md, kural 8).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Health_Response_ContainsNoAgentInformation()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health", UriKind.Relative), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotContain("agent", Case.Insensitive);
    }
}
