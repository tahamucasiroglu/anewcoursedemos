using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Default-deny yüzeyi (spec 0002 AC-11, Api tarafı): kimliksiz uçlar tam olarak Req 11 listesidir (giriş, kayıt, refresh —
/// A-0 —, sağlık, geçici ajan hub'ı); geri kalan her uç kimliksiz istekte 401 döner ve veri vermez.
/// </summary>
public sealed class AuthorizationSurfaceTests : IClassFixture<IdentityApiFactory>
{
    private static readonly string[] AllowList =
    [
        "GET health",
        "POST api/auth/login",
        "POST api/auth/refresh",
        "POST api/auth/register",
        "POST api/agent-pairing/codes",
        "POST api/agent-pairing/status",
        "* hubs/agent/negotiate",
        "* hubs/agent",
    ];

    private readonly IdentityApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="AuthorizationSurfaceTests"/> class.</summary>
    /// <param name="factory">Kimlik test fabrikası.</param>
    public AuthorizationSurfaceTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Kimliksiz erişime açık uçlar tam olarak Req 11'in istisna listesidir; fazlası da eksiği de yoktur.</summary>
    [Fact]
    public void AnonymousEndpoints_ExactlyReq11AllowList()
    {
        using var client = _factory.CreateClient();

        var anonymous = Endpoints().Where(e => e.Metadata.GetMetadata<IAllowAnonymous>() is not null).Select(Describe);

        anonymous.Order(StringComparer.Ordinal).ShouldBe(AllowList.Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// İstisna dışındaki her uç (iç uç dahil) kimliksiz istekte 401 döner ve gövdesinde kullanıcı/ajan verisi yoktur; geçersiz
    /// imzalı token da reddedilir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Anonymous_EveryProtectedApiEndpoint_Returns401()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var protectedEndpoints = Endpoints().Where(e => e.Metadata.GetMetadata<IAllowAnonymous>() is null).ToList();

        protectedEndpoints.Count.ShouldBeGreaterThanOrEqualTo(5);
        foreach (var endpoint in protectedEndpoints)
        {
            var method = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.FirstOrDefault() ?? "GET";
            var path = "/" + endpoint.RoutePattern.RawText!.TrimStart('/');
            using var anonymous = new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(new { }) };
            using var forged = new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(new { }) };
            forged.Headers.Authorization = new("Bearer", "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.c2FodGU");

            using var anonymousResponse = await client.SendAsync(anonymous, ct);
            using var forgedResponse = await client.SendAsync(forged, ct);
            var body = await anonymousResponse.Content.ReadAsStringAsync(ct);

            anonymousResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, $"{method} {path}");
            forgedResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, $"{method} {path}");
            body.ShouldNotContain("agent", Case.Insensitive);
            body.ShouldNotContain("lastEvent", Case.Insensitive);
        }
    }

    /// <summary>Sağlık kontrolü kimliksizdir ve yalnızca API, veritabanı ve sürüm döndürür.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Health_ContainsOnlyApiDbVersion()
    {
        using var client = _factory.CreateClient();

        var health = await client.GetFromJsonAsync<JsonElement>("/health", TestContext.Current.CancellationToken);

        health.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal).ShouldBe(["api", "database", "version"]);
    }

    private static string Describe(RouteEndpoint endpoint)
    {
        var method = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.FirstOrDefault() ?? "*";
        return $"{method} {endpoint.RoutePattern.RawText!.TrimStart('/')}";
    }

    private List<RouteEndpoint> Endpoints() =>
        [.. _factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()];
}
