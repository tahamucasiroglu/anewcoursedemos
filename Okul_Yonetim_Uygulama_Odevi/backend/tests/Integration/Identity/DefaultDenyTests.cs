using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OkulYonetim.Application.Ports;
using OkulYonetim.Infrastructure.Security;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Identity;

/// <summary>
/// Varsayılan reddet (spec 0004 AC-7): anonim uçlar tam olarak izinli liste; diğer her uç girişsiz 401. Süresi dolmuş,
/// bozuk ya da başka anahtarla imzalanmış anahtar reddedilir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DefaultDenyTests
{
    private static readonly string[] AllowedAnonymous = ["GET api/system-health", "POST api/auth/forgot-password", "POST api/auth/login", "POST api/auth/refresh", "POST api/auth/reset-password", "POST api/auth/reset-password/check", "POST api/auth/two-factor/setup", "POST api/auth/two-factor/setup/confirm", "POST api/auth/two-factor/verify"];
    private readonly TestDatabase database;

    public DefaultDenyTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task EveryEndpoint_ExceptAllowList_RejectsAnonymousWith401()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        List<(string Method, string Pattern, bool Anonymous)> endpoints = [.. factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText?.StartsWith("api/", StringComparison.Ordinal) == true)
            .SelectMany(e => (e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["GET"])
                .Select(m => (m, e.RoutePattern.RawText!, e.Metadata.GetMetadata<IAllowAnonymous>() is not null)))];

        endpoints.Where(e => e.Anonymous).Select(e => $"{e.Method} {e.Pattern}").Order().ShouldBe(AllowedAnonymous);
        endpoints.Count(e => !e.Anonymous).ShouldBeGreaterThanOrEqualTo(5);
        foreach ((string method, string pattern, _) in endpoints.Where(e => !e.Anonymous))
        {
            string path = "/" + pattern.Replace("{id:guid}", Guid.NewGuid().ToString(), StringComparison.Ordinal);
            using HttpRequestMessage request = new(new HttpMethod(method), new Uri(path, UriKind.Relative));
            if (method == "POST")
            {
                request.Content = System.Net.Http.Json.JsonContent.Create(new { tenantId = Guid.NewGuid() });
            }

            using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, $"{method} {path}");
        }
    }

    [Fact]
    public async Task Me_WithExpiredGarbageOrForeignSignedToken_Returns401()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed school = await CreateSchoolAsync(factory);
        JsonElement auth = await LoginAsync(client, school.FounderEmail);
        string valid = AccessToken(auth);
        string[] parts = valid.Split('.');
        ITokenService tokens = factory.Services.GetRequiredService<ITokenService>();
        Guid sessionId = Guid.Parse(System.Text.Json.JsonDocument.Parse(Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Decode(parts[1])).RootElement.GetProperty("sid").GetString()!);
        string expired = tokens.IssueAccessToken(school.FounderId, sessionId, school.TenantId, DateTimeOffset.UtcNow.AddHours(-1)).Token;
        JwtTokenService foreign = new(Options.Create(new JwtOptions { SigningKey = "baska-bir-sistemin-anahtari-en-az-32-bayt-9876543210" }));
        string foreignSigned = foreign.IssueAccessToken(school.FounderId, sessionId, school.TenantId, DateTimeOffset.UtcNow).Token;

        foreach (string token in new[] { expired, foreignSigned, "bozuk.anahtar.degeri", parts[0] + "." + parts[1] + ".imzasiz" })
        {
            using HttpResponseMessage response = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/me", token);
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        using HttpResponseMessage ok = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/me", valid);
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
