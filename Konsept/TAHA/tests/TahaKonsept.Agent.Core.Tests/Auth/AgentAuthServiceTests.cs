using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TahaKonsept.Agent.Core.Auth.Concrete;
using TahaKonsept.Agent.Core.Configuration;
using TahaKonsept.Agent.Core.Http.Concrete;
using TahaKonsept.Agent.Core.Tests.Support;
using TahaKonsept.Shared.Contracts.Identity;
using Xunit;

namespace TahaKonsept.Agent.Core.Tests.Auth;

/// <summary>Oturum servisi: süresi dolan token yenilenir; yenileme reddedilirse (web'den çıkış) oturum kapanır.</summary>
public sealed class AgentAuthServiceTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly PlainTokenStore _store;
    private readonly FakeSettingsStore _settings = new(new AgentSettings { ServerBaseUrl = "http://pi.test:7111" });

    /// <summary>Kurar.</summary>
    public AgentAuthServiceTests()
    {
        _store = new PlainTokenStore(new TestAgentPaths(_temp.Path));
    }

    /// <inheritdoc />
    public void Dispose() => _temp.Dispose();

    /// <summary>Erişim token'ı dolmuşsa yenilenir ve yeni token kaydedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task GetAccessToken_RefreshesExpiredToken()
    {
        _store.Save(Tokens("old", DateTimeOffset.UtcNow.AddMinutes(-1)));
        var handler = new FakeHttpMessageHandler((_, _) => FakeHttpMessageHandler.Json(HttpStatusCode.OK, Tokens("new", DateTimeOffset.UtcNow.AddMinutes(15))));
        var auth = Create(handler);

        var token = await auth.GetAccessTokenAsync(TestContext.Current.CancellationToken);

        token.ShouldBe("new");
        _store.Load()!.AccessToken.ShouldBe("new");
        handler.Requests.ShouldBe(["POST /api/identity/refresh"]);
    }

    /// <summary>Geçerli token yenilenmeden döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task GetAccessToken_ValidToken_NoRefresh()
    {
        _store.Save(Tokens("valid", DateTimeOffset.UtcNow.AddMinutes(10)));
        var handler = new FakeHttpMessageHandler((_, _) => throw new InvalidOperationException("çağrılmamalı"));
        var auth = Create(handler);

        (await auth.GetAccessTokenAsync(TestContext.Current.CancellationToken)).ShouldBe("valid");
        handler.Requests.ShouldBeEmpty();
    }

    /// <summary>Yenileme 401 ile reddedilirse token'lar silinir ve olay tetiklenir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Refresh_Rejected_SignsOut()
    {
        _store.Save(Tokens("old", DateTimeOffset.UtcNow.AddMinutes(-1)));
        var handler = new FakeHttpMessageHandler((_, _) => FakeHttpMessageHandler.Problem(HttpStatusCode.Unauthorized, "identity.refresh_invalid"));
        var auth = Create(handler);
        var changed = 0;
        auth.AuthorizationChanged += (_, _) => changed++;

        var refreshed = await auth.RefreshAsync(TestContext.Current.CancellationToken);

        refreshed.ShouldBeFalse();
        auth.IsPaired.ShouldBeFalse();
        _store.Load().ShouldBeNull();
        changed.ShouldBe(1);
    }

    /// <summary>Ağ hatasında token'lar korunur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Refresh_ServerDown_KeepsTokens()
    {
        _store.Save(Tokens("old", DateTimeOffset.UtcNow.AddMinutes(-1)));
        var handler = new FakeHttpMessageHandler((_, _) => throw new HttpRequestException("bağlantı yok"));
        var auth = Create(handler);

        var refreshed = await auth.RefreshAsync(TestContext.Current.CancellationToken);

        refreshed.ShouldBeFalse();
        auth.IsPaired.ShouldBeTrue();
        _store.Load().ShouldNotBeNull();
    }

    private static TokenResponse Tokens(string access, DateTimeOffset accessExpires) => new()
    {
        AccessToken = access,
        RefreshToken = "refresh-" + access,
        AccessTokenExpiresAtUtc = accessExpires,
        RefreshTokenExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(30),
    };

    private AgentAuthService Create(FakeHttpMessageHandler handler) =>
        new(_store, new AnonymousAgentApiClient(new HttpClient(handler), _settings), new AgentOptions(), TimeProvider.System, NullLogger<AgentAuthService>.Instance);
}
