using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Identity;

/// <summary>Oturum yenileme, 7 gün, çıkış (spec 0004 AC-8).</summary>
[Trait("Category", "Integration")]
public sealed class SessionTests
{
    private readonly TestDatabase database;

    public SessionTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Refresh_AfterAccessTokenExpired_IssuesWorkingTokenAndRotatesRefreshToken()
    {
        FakeTimeProvider time = new(DateTimeOffset.UtcNow);
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        SchoolSeed school = await CreateSchoolAsync(factory);
        JsonElement auth = await LoginAsync(client, school.FounderEmail);
        string firstRefresh = auth.GetProperty("refreshToken").GetString()!;

        time.Advance(TimeSpan.FromMinutes(16));
        using (HttpResponseMessage expired = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/me", AccessToken(auth)))
        {
            expired.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        JsonElement refreshed = await RefreshAsync(client, firstRefresh, HttpStatusCode.OK);
        using (HttpResponseMessage me = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/me", AccessToken(refreshed)))
        {
            me.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        refreshed.GetProperty("refreshToken").GetString().ShouldNotBe(firstRefresh);
        await RefreshAsync(client, firstRefresh, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_AfterSevenIdleDays_IsRejected_ButUseExtendsSession()
    {
        FakeTimeProvider time = new(DateTimeOffset.UtcNow);
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        SchoolSeed school = await CreateSchoolAsync(factory);
        JsonElement auth = await LoginAsync(client, school.FounderEmail);

        time.Advance(TimeSpan.FromDays(6));
        JsonElement used = await RefreshAsync(client, auth.GetProperty("refreshToken").GetString()!, HttpStatusCode.OK);
        time.Advance(TimeSpan.FromDays(6));
        JsonElement usedAgain = await RefreshAsync(client, used.GetProperty("refreshToken").GetString()!, HttpStatusCode.OK);
        time.Advance(TimeSpan.FromDays(7) + TimeSpan.FromMinutes(1));

        JsonElement rejected = await RefreshAsync(client, usedAgain.GetProperty("refreshToken").GetString()!, HttpStatusCode.Unauthorized);
        rejected.GetProperty("detail").GetString().ShouldBe("Oturumunuz sona erdi, tekrar giriş yapın.");
    }

    [Fact]
    public async Task Logout_RejectsOldAccessAndRefreshTokensImmediately()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();
        SchoolSeed school = await CreateSchoolAsync(factory);
        JsonElement auth = await LoginAsync(client, school.FounderEmail);

        using (HttpResponseMessage logout = await SendAuthorizedAsync(client, HttpMethod.Post, "/api/auth/logout", AccessToken(auth)))
        {
            logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        using (HttpResponseMessage me = await SendAuthorizedAsync(client, HttpMethod.Get, "/api/me", AccessToken(auth)))
        {
            me.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        await RefreshAsync(client, auth.GetProperty("refreshToken").GetString()!, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WebCookie_WorksWithoutBodyToken()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = true });
        SchoolSeed school = await CreateSchoolAsync(factory);
        using (HttpResponseMessage login = await PostLoginAsync(client, school.FounderEmail, Password, refreshTokenInBody: false))
        {
            login.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        using HttpResponseMessage refreshed = await client.PostAsJsonAsync(new Uri("/api/auth/refresh", UriKind.Relative), new { }, TestContext.Current.CancellationToken);

        refreshed.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadJsonAsync(refreshed)).GetProperty("refreshToken").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    private static async Task<JsonElement> RefreshAsync(HttpClient client, string refreshToken, HttpStatusCode expected)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(new Uri("/api/auth/refresh", UriKind.Relative), new { refreshToken }, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(expected);
        return await ReadJsonAsync(response);
    }
}
