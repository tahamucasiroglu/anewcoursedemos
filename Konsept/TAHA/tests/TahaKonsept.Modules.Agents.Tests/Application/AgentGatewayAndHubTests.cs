using Shouldly;
using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Modules.Agents.Domain.Entities;
using TahaKonsept.Modules.Agents.Domain.Errors;
using TahaKonsept.Modules.Agents.Tests.Support;
using TahaKonsept.Shared.Contracts.Agent;
using Xunit;

namespace TahaKonsept.Modules.Agents.Tests.Application;

/// <summary>
/// Hub servisi + ajan kapısı birlikte: bağlantı/Hello ile cihaz eşleme ve şifreli anahtar, çevrimdışı hataları, video adresi,
/// komut gönderimi, ses önizleme yanıt eşleme ve zaman aşımı, iptal edilmiş cihazın reddi, bağlantı kopunca çevrimdışı, eşleme kaldırma.
/// </summary>
public sealed class AgentGatewayAndHubTests : IDisposable
{
    private readonly AgentsTestHarness _h = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Ajan yokken kapı çevrimdışı hatası verir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Gateway_reports_offline_without_connection()
    {
        var gateway = _h.CreateGateway();

        gateway.IsAgentOnline(_h.UserId).ShouldBeFalse();
        (await gateway.GetOnlineAgentAsync(_h.UserId, Ct)).ShouldBeNull();
        (await gateway.SendStageAsync(_h.UserId, new StageCommand(), Ct)).Error.ShouldBe(AgentErrors.AgentOffline);
        gateway.GetVideoUrl(_h.UserId, Guid.NewGuid()).Error.ShouldBe(AgentErrors.AgentOffline);
    }

    /// <summary>Hello öncesi adres bilinmiyor; Hello sonrası video adresi kurulur, anahtar DB'de şifreli saklanır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Hello_by_machine_name_enables_video_url()
    {
        var device = await SeedDeviceAsync();
        var hub = _h.CreateHubService();
        var gateway = _h.CreateGateway();
        (await hub.ConnectedAsync(Connection("c1", deviceId: null), Ct)).IsSuccess.ShouldBeTrue();

        gateway.GetVideoUrl(_h.UserId, Guid.NewGuid()).Error.ShouldBe(AgentErrors.AgentAddressUnknown);

        (await hub.HelloAsync(Call("c1"), Hello(), Ct)).IsSuccess.ShouldBeTrue();

        var videoId = Guid.NewGuid();
        gateway.GetVideoUrl(_h.UserId, videoId).Value.ShouldBe($"http://192.168.1.20:7120/videos/{videoId:D}?k=s3cret");
        _h.Tracker.Get("c1")!.DeviceId.ShouldBe(device.Id);
        var stored = await _h.Devices.GetByIdAsync(device.Id, Ct);
        stored!.StreamKeyProtected.ShouldBe("enc:s3cret");
        stored.IsOnline.ShouldBeTrue();
        stored.CapabilitiesJson.ShouldNotBeNull();

        var info = await gateway.GetOnlineAgentAsync(_h.UserId, Ct);
        info!.Id.ShouldBe(device.Id);
        info.GpuName.ShouldBe("RTX 3060 Laptop");
        info.VramMb.ShouldBe(6144);
    }

    /// <summary>Komutlar birincil bağlantıya gider.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Commands_are_sent_to_primary_connection()
    {
        await ConnectAsync();
        var gateway = _h.CreateGateway();
        var jobId = Guid.NewGuid();

        (await gateway.SendStageAsync(_h.UserId, new StageCommand { JobId = jobId }, Ct)).IsSuccess.ShouldBeTrue();
        (await gateway.CancelJobAsync(_h.UserId, jobId, Ct)).IsSuccess.ShouldBeTrue();

        _h.Clients.LastConnectionId.ShouldBe("c1");
        _h.Clients.Client.Calls.Select(c => c.Method).ShouldBe(["ExecuteStage", "CancelJob"]);
    }

    /// <summary>Gönderim istisnası <c>agents.send_failed</c> olur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Send_failure_is_reported()
    {
        await ConnectAsync();
        _h.Clients.Client.ThrowOnSend = new InvalidOperationException("kapalı");

        var result = await _h.CreateGateway().OpenFolderAsync(_h.UserId, Guid.NewGuid(), Ct);

        result.Error.Code.ShouldBe(AgentErrors.SendFailed.Code);
    }

    /// <summary>Ses önizleme: ajan yanıtı istek kimliğiyle eşlenir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Preview_speech_returns_agent_result()
    {
        await ConnectAsync();
        var hub = _h.CreateHubService();
        _h.Clients.Client.OnPreviewSpeech = cmd => _ = Task.Run(
            () => hub.SpeechPreviewCompletedAsync(Call("c1"), new SpeechPreviewResult { RequestId = cmd.RequestId, Success = true, AudioPath = "/previews/1.wav" }),
            Ct);

        var result = await _h.CreateGateway().PreviewSpeechAsync(_h.UserId, new SpeechPreviewCommand { Text = "Gugıl" }, TimeSpan.FromSeconds(5), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.AudioPath.ShouldBe("/previews/1.wav");
        _h.Previews.PendingCount.ShouldBe(0);
    }

    /// <summary>Yanıt gelmezse zaman aşımı hatası, bekleyen istek temizlenir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Preview_speech_times_out()
    {
        await ConnectAsync();

        var result = await _h.CreateGateway().PreviewSpeechAsync(_h.UserId, new SpeechPreviewCommand { Text = "x" }, TimeSpan.FromMilliseconds(100), Ct);

        result.Error.ShouldBe(AgentErrors.PreviewTimeout);
        _h.Previews.PendingCount.ShouldBe(0);
    }

    /// <summary>İptal edilmiş ya da başkasına ait cihaz claim'iyle bağlantı reddedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Revoked_device_connection_is_rejected()
    {
        var device = await SeedDeviceAsync();
        device.Revoke(_h.Clock.UtcNow);
        await _h.Db.SaveChangesAsync(Ct);

        var result = await _h.CreateHubService().ConnectedAsync(Connection("c9", device.Id), Ct);

        result.IsFailure.ShouldBeTrue();
        _h.Tracker.Get("c9").ShouldBeNull();
    }

    /// <summary>Son bağlantı kopunca cihaz çevrimdışı işaretlenir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Disconnect_marks_device_offline()
    {
        var device = await ConnectAsync();

        await _h.CreateHubService().DisconnectedAsync("c1", Ct);

        (await _h.Devices.GetByIdAsync(device.Id, Ct))!.IsOnline.ShouldBeFalse();
        _h.CreateGateway().IsAgentOnline(_h.UserId).ShouldBeFalse();
    }

    /// <summary>Eşleme kaldırılınca cihaz listeden düşer, aile iptal edilir, bağlantı kesilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Removing_device_revokes_and_disconnects()
    {
        var aborted = false;
        var device = await SeedDeviceAsync();
        device.AssignTokenFamily(Guid.NewGuid());
        await _h.Db.SaveChangesAsync(Ct);
        await _h.CreateHubService().ConnectedAsync(new AgentConnection("c1", _h.UserId, device.Id, _h.Clock.UtcNow, () => aborted = true), Ct);
        var service = _h.CreateDeviceService();

        (await service.ListAsync(Ct)).Value.Single().IsOnline.ShouldBeTrue();
        (await service.RemoveAsync(device.Id, Ct)).IsSuccess.ShouldBeTrue();

        aborted.ShouldBeTrue();
        _h.Tokens.RevokedFamilies.ShouldBe([device.TokenFamilyId!.Value]);
        (await service.ListAsync(Ct)).Value.ShouldBeEmpty();
        (await service.RemoveAsync(device.Id, Ct)).Error.ShouldBe(AgentErrors.DeviceNotFound);
    }

    /// <summary>Kaynakları bırakır.</summary>
    public void Dispose() => _h.Dispose();

    private static AgentHello Hello() => new()
    {
        AgentVersion = "0.3.0",
        MachineName = "TAHA-PC",
        LanIpAddress = "192.168.1.20",
        VideoServerPort = 7120,
        StreamKey = "s3cret",
        Capabilities = new AgentCapabilities { Gpus = [new GpuInfo { Name = "RTX 3060 Laptop", VramMb = 6144 }] },
    };

    private AgentCallContext Call(string connectionId) => new(connectionId, _h.UserId, null);

    private AgentConnection Connection(string connectionId, Guid? deviceId) =>
        new(connectionId, _h.UserId, deviceId, _h.Clock.UtcNow, () => { });

    private async Task<AgentDevice> SeedDeviceAsync()
    {
        var device = AgentDevice.Pair(_h.UserId, "TAHA-PC", "0.3.0", _h.Clock.UtcNow);
        await _h.Devices.AddAsync(device, Ct);
        await _h.Db.SaveChangesAsync(Ct);
        return device;
    }

    private async Task<AgentDevice> ConnectAsync()
    {
        var device = await SeedDeviceAsync();
        var hub = _h.CreateHubService();
        (await hub.ConnectedAsync(Connection("c1", device.Id), Ct)).IsSuccess.ShouldBeTrue();
        (await hub.HelloAsync(Call("c1"), Hello(), Ct)).IsSuccess.ShouldBeTrue();
        return device;
    }
}
