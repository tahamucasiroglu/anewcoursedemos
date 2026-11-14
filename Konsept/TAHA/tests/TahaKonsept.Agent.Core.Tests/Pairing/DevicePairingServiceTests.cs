using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TahaKonsept.Agent.Core.Auth.Concrete;
using TahaKonsept.Agent.Core.Configuration;
using TahaKonsept.Agent.Core.Http.Concrete;
using TahaKonsept.Agent.Core.Pairing;
using TahaKonsept.Agent.Core.Pairing.Concrete;
using TahaKonsept.Agent.Core.Tests.Support;
using TahaKonsept.Shared.Contracts.Agent.Pairing;
using TahaKonsept.Shared.Contracts.Identity;
using Xunit;

namespace TahaKonsept.Agent.Core.Tests.Pairing;

/// <summary>
/// Cihaz eşleme durum makinesi (sahte HTTP ile gerçek istemci + oturum servisi): bekleme → yavaşla → onay, reddetme,
/// süre dolması, başlatma hatası, iptal.
/// </summary>
public sealed class DevicePairingServiceTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly TestAgentPaths _paths;
    private readonly AgentOptions _options = new() { PairingSlowDownStep = TimeSpan.Zero };

    /// <summary>Geçici klasörleri kurar.</summary>
    public DevicePairingServiceTests()
    {
        _paths = new TestAgentPaths(_temp.Path);
    }

    /// <inheritdoc />
    public void Dispose() => _temp.Dispose();

    /// <summary>Bekleme ve yavaşla yanıtlarından sonra onaylanınca token'lar kaydedilir ve durum Paired olur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pair_PendingThenSlowDownThenApproved_StoresTokens()
    {
        var handler = new FakeHttpMessageHandler((request, index) => index switch
        {
            0 => FakeHttpMessageHandler.Json(HttpStatusCode.OK, Start()),
            1 => FakeHttpMessageHandler.Problem(HttpStatusCode.BadRequest, DeviceTokenErrorCodes.AuthorizationPending),
            2 => FakeHttpMessageHandler.Problem(HttpStatusCode.BadRequest, DeviceTokenErrorCodes.SlowDown),
            _ => FakeHttpMessageHandler.Json(HttpStatusCode.OK, Tokens()),
        });
        var (service, auth, store) = Create(handler);
        var states = new List<PairingState>();
        service.StatusChanged += (_, s) => states.Add(s.State);

        var result = await service.PairAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        service.Status.State.ShouldBe(PairingState.Paired);
        states.ShouldBe([PairingState.Starting, PairingState.WaitingForUser, PairingState.Paired]);
        auth.IsPaired.ShouldBeTrue();
        store.Load()!.RefreshToken.ShouldBe("refresh-1");
        handler.Requests.ShouldBe(
        [
            "POST /api/agents/device/start",
            "POST /api/agents/device/token",
            "POST /api/agents/device/token",
            "POST /api/agents/device/token",
        ]);
    }

    /// <summary>Başlatınca kullanıcı kodu durumda görünür.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Start_ExposesUserCode()
    {
        var handler = new FakeHttpMessageHandler((_, _) => FakeHttpMessageHandler.Json(HttpStatusCode.OK, Start()));
        var (service, _, _) = Create(handler);

        var start = await service.StartAsync(TestContext.Current.CancellationToken);

        start.IsSuccess.ShouldBeTrue();
        service.Status.State.ShouldBe(PairingState.WaitingForUser);
        service.Status.UserCode.ShouldBe("ABCD-EFGH");
        service.Status.VerificationUrl.ShouldBe("http://pi:7110/Ayarlar");
    }

    /// <summary>Reddedilirse Denied ve hata döner; token kaydedilmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pair_Denied()
    {
        var handler = new FakeHttpMessageHandler((_, index) => index == 0
            ? FakeHttpMessageHandler.Json(HttpStatusCode.OK, Start())
            : FakeHttpMessageHandler.Problem(HttpStatusCode.BadRequest, DeviceTokenErrorCodes.AccessDenied));
        var (service, auth, _) = Create(handler);

        var result = await service.PairAsync(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(DeviceTokenErrorCodes.AccessDenied);
        service.Status.State.ShouldBe(PairingState.Denied);
        auth.IsPaired.ShouldBeFalse();
    }

    /// <summary>Süre dolarsa Expired.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pair_Expired()
    {
        var handler = new FakeHttpMessageHandler((_, index) => index == 0
            ? FakeHttpMessageHandler.Json(HttpStatusCode.OK, Start())
            : FakeHttpMessageHandler.Problem(HttpStatusCode.BadRequest, DeviceTokenErrorCodes.ExpiredToken));
        var (service, _, _) = Create(handler);

        var result = await service.PairAsync(TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe(DeviceTokenErrorCodes.ExpiredToken);
        service.Status.State.ShouldBe(PairingState.Expired);
    }

    /// <summary>Başlatma sunucu hatası verirse Failed.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Start_ServerError_Failed()
    {
        var handler = new FakeHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var (service, _, _) = Create(handler);

        var result = await service.StartAsync(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        service.Status.State.ShouldBe(PairingState.Failed);
    }

    /// <summary>Kullanıcı vazgeçerse iptal fırlatılır ve durum Cancelled olur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Wait_Cancelled()
    {
        var handler = new FakeHttpMessageHandler((_, _) => FakeHttpMessageHandler.Problem(HttpStatusCode.BadRequest, DeviceTokenErrorCodes.AuthorizationPending));
        var (service, _, _) = Create(handler);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(300));

        await Should.ThrowAsync<OperationCanceledException>(() => service.WaitForApprovalAsync(Start() with { IntervalSeconds = 0 }, cts.Token));

        service.Status.State.ShouldBe(PairingState.Cancelled);
    }

    /// <summary>Eşleme kaldırılınca token'lar silinir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Unpair_ClearsTokens()
    {
        var handler = new FakeHttpMessageHandler((_, index) => index == 0
            ? FakeHttpMessageHandler.Json(HttpStatusCode.OK, Start())
            : FakeHttpMessageHandler.Json(HttpStatusCode.OK, Tokens()));
        var (service, auth, store) = Create(handler);
        (await service.PairAsync(TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        service.Unpair();

        auth.IsPaired.ShouldBeFalse();
        store.Load().ShouldBeNull();
        service.Status.State.ShouldBe(PairingState.NotPaired);
    }

    private static DeviceStartResponse Start() => new()
    {
        DeviceCode = "device-secret",
        UserCode = "ABCD-EFGH",
        VerificationUrl = "http://pi:7110/Ayarlar",
        IntervalSeconds = 0,
        ExpiresInSeconds = 600,
    };

    private static TokenResponse Tokens() => new()
    {
        AccessToken = "access-1",
        RefreshToken = "refresh-1",
        AccessTokenExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(15),
        RefreshTokenExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(30),
    };

    private (DevicePairingService Service, AgentAuthService Auth, PlainTokenStore Store) Create(FakeHttpMessageHandler handler)
    {
        var settings = new FakeSettingsStore(new AgentSettings { ServerBaseUrl = "http://pi.test:7111", StreamKey = "k" });
        var api = new AnonymousAgentApiClient(new HttpClient(handler), settings);
        var store = new PlainTokenStore(_paths);
        var auth = new AgentAuthService(store, api, _options, TimeProvider.System, NullLogger<AgentAuthService>.Instance);
        var service = new DevicePairingService(api, auth, _options, TimeProvider.System, NullLogger<DevicePairingService>.Instance);
        return (service, auth, store);
    }
}
