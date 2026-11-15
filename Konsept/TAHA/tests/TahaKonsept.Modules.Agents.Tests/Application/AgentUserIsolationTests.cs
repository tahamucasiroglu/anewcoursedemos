using Shouldly;
using TahaKonsept.BuildingBlocks.Infrastructure.Storage.Concrete;
using TahaKonsept.Modules.Agents.Application.Concrete;
using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Modules.Agents.Domain.Entities;
using TahaKonsept.Modules.Agents.Domain.Errors;
using TahaKonsept.Modules.Agents.Infrastructure.Persistence.Concrete;
using TahaKonsept.Modules.Agents.Infrastructure.Storage.Concrete;
using TahaKonsept.Modules.Agents.Tests.Support;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Agent.Pairing;
using Xunit;

namespace TahaKonsept.Modules.Agents.Tests.Application;

/// <summary>
/// Kullanıcı ayrımı (A ve B): B, A'nın cihazını göremez/kaldıramaz, A'nın cihaz kimliğiyle bağlanamaz; komutlar yalnız sahibin
/// bağlantısına gider; ses önizleme yanıtı yalnız sahibin ajanından kabul edilir; A'nın artefaktı B için yoktur (NotFound);
/// eşleme onaylayan kullanıcıya bağlanır.
/// </summary>
public sealed class AgentUserIsolationTests : IDisposable
{
    private readonly AgentsTestHarness _h = new();
    private readonly Guid _userB = Guid.CreateVersion7();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "tk-artifacts-" + Guid.NewGuid().ToString("N"));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>B, A'nın cihazını listede görmez ve kaldıramaz (NotFound); A'nın cihazı etkilenmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Other_user_cannot_list_or_remove_device()
    {
        var device = await SeedDeviceAsync(_h.UserId, "A-PC");

        _h.User.UserId = _userB;
        var service = _h.CreateDeviceService();
        (await service.ListAsync(Ct)).Value.ShouldBeEmpty();
        (await service.RemoveAsync(device.Id, Ct)).Error.ShouldBe(AgentErrors.DeviceNotFound);

        _h.User.UserId = _h.UserId;
        (await _h.CreateDeviceService().ListAsync(Ct)).Value.ShouldHaveSingleItem().Id.ShouldBe(device.Id);
        (await _h.Devices.GetByIdAsync(device.Id, Ct))!.IsRevoked.ShouldBeFalse();
    }

    /// <summary>B'nin token'ı A'nın cihaz kimliğini taşısa da bağlantı reddedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Other_user_cannot_connect_as_owners_device()
    {
        var device = await SeedDeviceAsync(_h.UserId, "A-PC");

        var result = await _h.CreateHubService().ConnectedAsync(Connection("b1", _userB, device.Id), Ct);

        result.Error.ShouldBe(AgentHubService.DeviceRevokedError);
        _h.Tracker.Get("b1").ShouldBeNull();
        _h.CreateGateway().IsAgentOnline(_userB).ShouldBeFalse();
    }

    /// <summary>A ve B aynı anda bağlıyken komutlar yalnız ilgili kullanıcının bağlantısına gider; ajanı olmayan kullanıcı çevrimdışıdır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Commands_route_only_to_owners_connection()
    {
        var hub = _h.CreateHubService();
        var deviceA = await SeedDeviceAsync(_h.UserId, "A-PC");
        var deviceB = await SeedDeviceAsync(_userB, "B-PC");
        (await hub.ConnectedAsync(Connection("a1", _h.UserId, deviceA.Id), Ct)).IsSuccess.ShouldBeTrue();
        (await hub.ConnectedAsync(Connection("b1", _userB, deviceB.Id), Ct)).IsSuccess.ShouldBeTrue();
        var gateway = _h.CreateGateway();

        (await gateway.SendStageAsync(_userB, new StageCommand(), Ct)).IsSuccess.ShouldBeTrue();
        _h.Clients.LastConnectionId.ShouldBe("b1");

        (await gateway.CancelJobAsync(_h.UserId, Guid.NewGuid(), Ct)).IsSuccess.ShouldBeTrue();
        _h.Clients.LastConnectionId.ShouldBe("a1");

        var stranger = Guid.CreateVersion7();
        gateway.IsAgentOnline(stranger).ShouldBeFalse();
        (await gateway.DeleteVideoAsync(stranger, Guid.NewGuid(), Ct)).Error.ShouldBe(AgentErrors.AgentOffline);
        (await gateway.GetOnlineAgentAsync(_userB, Ct))!.Id.ShouldBe(deviceB.Id);
    }

    /// <summary>B'nin ajanı A'nın bekleyen ses önizleme isteğini tamamlayamaz; istek A'nın yanıtını beklemeye devam eder.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Preview_result_from_other_users_agent_is_ignored()
    {
        var hub = _h.CreateHubService();
        var requestId = Guid.CreateVersion7();
        var pending = _h.Previews.Register(_h.UserId, requestId);

        await hub.SpeechPreviewCompletedAsync(new AgentCallContext("b1", _userB, null), new SpeechPreviewResult { RequestId = requestId, Success = true, AudioPath = "/sahte.wav" });

        pending.IsCompleted.ShouldBeFalse();
        _h.Previews.PendingCount.ShouldBe(1);

        await hub.SpeechPreviewCompletedAsync(new AgentCallContext("a1", _h.UserId, null), new SpeechPreviewResult { RequestId = requestId, Success = true, AudioPath = "/gercek.wav" });

        (await pending.WaitAsync(TimeSpan.FromSeconds(5), Ct)).AudioPath.ShouldBe("/gercek.wav");
        _h.Previews.PendingCount.ShouldBe(0);
    }

    /// <summary>A'nın artefaktı B için yoktur: indirme ve onay (silme) NotFound döner, dosya A için yerinde kalır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Other_user_cannot_download_or_delete_artifact()
    {
        var store = new FileSystemArtifactStore(new AppDataLocation(_root), new StoredArtifactRepository(_h.Db));
        var saved = (await store.SaveAsync(_h.UserId, Guid.NewGuid(), "a.png", "image/png", new MemoryStream([1, 2, 3]), 0, Ct)).Value;

        _h.User.UserId = _userB;
        var asB = new AgentArtifactService(store, _h.User, _h.Clock);
        (await asB.DownloadAsync(saved.ArtifactId, Ct)).Error.ShouldBe(AgentErrors.ArtifactNotFound);
        (await asB.AcknowledgeAsync(saved.ArtifactId, Ct)).Error.ShouldBe(AgentErrors.ArtifactNotFound);
        (await store.DeleteJobArtifactsAsync(_userB, Guid.NewGuid(), Ct)).Value.ShouldBe(0);

        _h.User.UserId = _h.UserId;
        var opened = await new AgentArtifactService(store, _h.User, _h.Clock).DownloadAsync(saved.ArtifactId, Ct);
        opened.IsSuccess.ShouldBeTrue();
        await opened.Value.Content.DisposeAsync();
    }

    /// <summary>Eşleme kodunu B onaylarsa cihaz ve token B'ye bağlanır; A'nın cihaz listesi boş kalır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pairing_binds_device_to_approving_user()
    {
        var service = _h.CreatePairingService();
        var start = (await service.StartAsync(new DeviceStartRequest { MachineName = "B-PC", AgentVersion = "0.3.0" }, Ct)).Value;

        _h.User.UserId = _userB;
        (await service.ApproveAsync(new DeviceApproveRequest { UserCode = start.UserCode }, Ct)).IsSuccess.ShouldBeTrue();
        _h.Clock.Advance(TimeSpan.FromSeconds(6));
        (await service.PollTokenAsync(new DeviceTokenRequest { DeviceCode = start.DeviceCode }, Ct)).IsSuccess.ShouldBeTrue();

        _h.Tokens.Issued.Single().UserId.ShouldBe(_userB);
        (await _h.Devices.ListActiveAsync(_userB, Ct)).ShouldHaveSingleItem().MachineName.ShouldBe("B-PC");
        (await _h.Devices.ListActiveAsync(_h.UserId, Ct)).ShouldBeEmpty();
    }

    /// <summary>Kaynakları bırakır.</summary>
    public void Dispose()
    {
        _h.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private AgentConnection Connection(string connectionId, Guid userId, Guid? deviceId) =>
        new(connectionId, userId, deviceId, _h.Clock.UtcNow, () => { }) { ClientType = AgentClaimTypes.AgentClientValue };

    private async Task<AgentDevice> SeedDeviceAsync(Guid userId, string machineName)
    {
        var device = AgentDevice.Pair(userId, machineName, "0.3.0", _h.Clock.UtcNow);
        await _h.Devices.AddAsync(device, Ct);
        await _h.Db.SaveChangesAsync(Ct);
        return device;
    }
}
