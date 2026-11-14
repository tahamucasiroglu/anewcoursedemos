using Shouldly;
using TahaKonsept.Modules.Agents.Domain.Errors;
using TahaKonsept.Modules.Agents.Tests.Support;
using TahaKonsept.Shared.Contracts.Agent.Pairing;
using Xunit;

namespace TahaKonsept.Modules.Agents.Tests.Application;

/// <summary>
/// <c>DevicePairingService</c> uçtan uca (EF InMemory): başlat → bekleniyor → onay → token bir kez; yavaşla; süre dolması; ret;
/// bilinmeyen kod; aynı bilgisayarın yeniden eşlenmesi (cihaz yeniden kullanılır, eski aile iptal edilir).
/// </summary>
public sealed class DevicePairingServiceTests : IDisposable
{
    private readonly AgentsTestHarness _h = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Başlatma: XXXX-XXXX kod, doğrulama adresleri, aralık ve süre.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Start_returns_codes_and_urls()
    {
        var start = await StartAsync();

        start.UserCode.ShouldMatch("^[A-Z2-9]{4}-[A-Z2-9]{4}$");
        start.DeviceCode.Length.ShouldBeGreaterThan(30);
        start.VerificationUrl.ShouldBe(StaticVerificationUrlProvider.Url);
        start.VerificationUrlComplete.ShouldBe("http://tmpi:7110/Ayarlar?kod=" + start.UserCode + "#masaustu");
        start.IntervalSeconds.ShouldBe(5);
        start.ExpiresInSeconds.ShouldBe(600);
    }

    /// <summary>Onay öncesi yoklama "authorization_pending"; onaydan sonra token bir kez verilir, ikinci yoklama "expired_token".</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pending_then_approved_then_token_once()
    {
        var service = _h.CreatePairingService();
        var start = await StartAsync();

        _h.Clock.Advance(TimeSpan.FromSeconds(5));
        var pending = await service.PollTokenAsync(new DeviceTokenRequest { DeviceCode = start.DeviceCode }, Ct);
        pending.Error.Code.ShouldBe(DeviceTokenErrorCodes.AuthorizationPending);

        var approved = await service.ApproveAsync(new DeviceApproveRequest { UserCode = start.UserCode.ToLowerInvariant() }, Ct);
        approved.IsSuccess.ShouldBeTrue();
        approved.Value.MachineName.ShouldBe("TAHA-PC");

        _h.Clock.Advance(TimeSpan.FromSeconds(6));
        var token = await service.PollTokenAsync(new DeviceTokenRequest { DeviceCode = start.DeviceCode }, Ct);
        token.IsSuccess.ShouldBeTrue();
        token.Value.AccessToken.ShouldBe("access-1");
        _h.Tokens.Issued.Single().UserId.ShouldBe(_h.UserId);

        var devices = await _h.Devices.ListActiveAsync(_h.UserId, Ct);
        devices.Count.ShouldBe(1);
        devices[0].TokenFamilyId.ShouldNotBeNull();

        _h.Clock.Advance(TimeSpan.FromSeconds(6));
        var again = await service.PollTokenAsync(new DeviceTokenRequest { DeviceCode = start.DeviceCode }, Ct);
        again.Error.Code.ShouldBe(DeviceTokenErrorCodes.ExpiredToken);
        _h.Tokens.Issued.Count.ShouldBe(1);
    }

    /// <summary>Onaylı isteği aynı anda iki yoklama tüketmeye çalışırsa yalnız biri token alır (ikinci aile oluşmaz).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Concurrent_polls_issue_tokens_once()
    {
        var service = _h.CreatePairingService();
        var (other, otherPairings) = _h.CreateIsolatedPairingService();
        var start = await StartAsync();
        (await service.ApproveAsync(new DeviceApproveRequest { UserCode = start.UserCode }, Ct)).IsSuccess.ShouldBeTrue();
        (await otherPairings.FindByDeviceCodeHashAsync(_h.Codes.HashDeviceCode(start.DeviceCode), Ct)).ShouldNotBeNull();

        _h.Clock.Advance(TimeSpan.FromSeconds(6));
        var first = await service.PollTokenAsync(new DeviceTokenRequest { DeviceCode = start.DeviceCode }, Ct);
        var second = await other.PollTokenAsync(new DeviceTokenRequest { DeviceCode = start.DeviceCode }, Ct);

        first.IsSuccess.ShouldBeTrue();
        second.Error.Code.ShouldBe(DeviceTokenErrorCodes.ExpiredToken);
        _h.Tokens.Issued.Count.ShouldBe(1);
    }

    /// <summary>Açık bekleyen eşleme sınırı dolunca yeni istek reddedilir (anonim uç DB'yi dolduramaz).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Open_pairing_cap_rejects_new_start()
    {
        _h.Options.MaxOpenPairings = 2;
        var service = _h.CreatePairingService();
        await StartAsync();
        await StartAsync();

        var third = await service.StartAsync(new DeviceStartRequest { MachineName = "X", AgentVersion = "0.3.0" }, Ct);

        third.Error.ShouldBe(AgentErrors.TooManyOpenPairings);
        (await _h.Pairings.CountAsync(null, Ct)).ShouldBe(2);
    }

    /// <summary>Aralıktan sık yoklama "slow_down" döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Fast_polling_returns_slow_down()
    {
        var service = _h.CreatePairingService();
        var start = await StartAsync();
        var request = new DeviceTokenRequest { DeviceCode = start.DeviceCode };

        _h.Clock.Advance(TimeSpan.FromSeconds(5));
        (await service.PollTokenAsync(request, Ct)).Error.Code.ShouldBe(DeviceTokenErrorCodes.AuthorizationPending);
        _h.Clock.Advance(TimeSpan.FromSeconds(1));
        (await service.PollTokenAsync(request, Ct)).Error.Code.ShouldBe(DeviceTokenErrorCodes.SlowDown);
    }

    /// <summary>Süre dolunca yoklama "expired_token" ve onay "user_code_expired" döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Expired_code_cannot_be_used()
    {
        var service = _h.CreatePairingService();
        var start = await StartAsync();

        _h.Clock.Advance(TimeSpan.FromMinutes(11));
        (await service.PollTokenAsync(new DeviceTokenRequest { DeviceCode = start.DeviceCode }, Ct))
            .Error.Code.ShouldBe(DeviceTokenErrorCodes.ExpiredToken);
        (await service.ApproveAsync(new DeviceApproveRequest { UserCode = start.UserCode }, Ct))
            .Error.Code.ShouldBe("agents.device.user_code_expired");
    }

    /// <summary>Ret sonrası yoklama "access_denied".</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Denied_code_returns_access_denied()
    {
        var service = _h.CreatePairingService();
        var start = await StartAsync();

        (await service.DenyAsync(new DeviceApproveRequest { UserCode = start.UserCode }, Ct)).IsSuccess.ShouldBeTrue();
        _h.Clock.Advance(TimeSpan.FromSeconds(5));
        (await service.PollTokenAsync(new DeviceTokenRequest { DeviceCode = start.DeviceCode }, Ct))
            .Error.Code.ShouldBe(DeviceTokenErrorCodes.AccessDenied);
    }

    /// <summary>Bilinmeyen kod ve bilinmeyen cihaz kodu hata verir; oturumsuz onay yetkisiz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Unknown_codes_and_anonymous_approval_fail()
    {
        var service = _h.CreatePairingService();

        (await service.ApproveAsync(new DeviceApproveRequest { UserCode = "ZZZZ-9999" }, Ct)).Error.Code.ShouldBe("agents.device.user_code_not_found");
        (await service.PollTokenAsync(new DeviceTokenRequest { DeviceCode = "yok" }, Ct)).Error.Code.ShouldBe(DeviceTokenErrorCodes.ExpiredToken);

        _h.User.UserId = null;
        (await service.ApproveAsync(new DeviceApproveRequest { UserCode = "ABCD-2345" }, Ct)).Error.Code.ShouldBe("agents.unauthorized");
    }

    /// <summary>Aynı bilgisayar yeniden eşlenince cihaz kaydı yeniden kullanılır ve eski token ailesi iptal edilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Repairing_same_machine_reuses_device_and_revokes_old_family()
    {
        await PairAsync();
        var firstFamily = (await _h.Devices.ListActiveAsync(_h.UserId, Ct)).Single().TokenFamilyId;

        await PairAsync();

        var devices = await _h.Devices.ListActiveAsync(_h.UserId, Ct);
        devices.Count.ShouldBe(1);
        devices[0].TokenFamilyId.ShouldNotBe(firstFamily);
        _h.Tokens.RevokedFamilies.ShouldBe([firstFamily!.Value]);
    }

    /// <summary>Kaynakları bırakır.</summary>
    public void Dispose() => _h.Dispose();

    private async Task<DeviceStartResponse> StartAsync()
    {
        var result = await _h.CreatePairingService().StartAsync(new DeviceStartRequest { MachineName = "TAHA-PC", AgentVersion = "0.3.0" }, Ct);
        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    private async Task PairAsync()
    {
        var service = _h.CreatePairingService();
        var start = await StartAsync();
        (await service.ApproveAsync(new DeviceApproveRequest { UserCode = start.UserCode }, Ct)).IsSuccess.ShouldBeTrue();
        _h.Clock.Advance(TimeSpan.FromSeconds(6));
        (await service.PollTokenAsync(new DeviceTokenRequest { DeviceCode = start.DeviceCode }, Ct)).IsSuccess.ShouldBeTrue();
    }
}
