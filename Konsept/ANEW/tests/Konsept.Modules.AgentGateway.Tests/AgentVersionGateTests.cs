using Konsept.Modules.AgentGateway.Application;
using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Application.Concrete;
using Konsept.Modules.AgentGateway.Contracts.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.AgentGateway.Domain.Entities;
using Konsept.Modules.AgentGateway.Domain.Rules;
using Konsept.Modules.AgentGateway.Infrastructure.Concrete;
using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Shared.Kernel.Abstract;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Konsept.Modules.AgentGateway.Tests;

/// <summary>
/// Sürüm kapısı (spec 0023 Req 3, 4, 15; AC-3, AC-6): ürün sürümünden farklı ajana hiçbir komut gitmez, "Ajanı test et" reddedilir,
/// durum "Ajan güncellemesi gerekli" der; güncellemeye başlayan ajana iş gitmez; "Üretiliyor" iş varken güncelleme başlamaz.
/// </summary>
public sealed class AgentVersionGateTests
{
    private const string ServerVersion = "0.5.0";
    private readonly InMemoryAgentConnectionRegistry _registry = new();
    private readonly IAgentChannel _channel = Substitute.For<IAgentChannel>();
    private readonly IProductionActivityReader _activity = Substitute.For<IProductionActivityReader>();
    private readonly FakeTimeProvider _time = new();
    private readonly AgentOwner _owner = new(Guid.NewGuid(), Guid.NewGuid());

    /// <summary>Kapı +sha ekini yok sayar, farklı sürümde güncelleme ister, kapalıyken her sürümü kabul eder.</summary>
    [Fact]
    public void Gate_ComparesNormalizedVersions()
    {
        var gate = new AgentVersionGate("0.5.0+abc");

        gate.ServerVersion.ShouldBe("0.5.0");
        gate.RequiresUpdate("0.5.0+def").ShouldBeFalse();
        gate.RequiresUpdate("0.4.0").ShouldBeTrue();
        gate.RequiresUpdate("0.6.0").ShouldBeTrue();
        gate.RequiresUpdate(null).ShouldBeTrue();
        new AgentVersionGate(ServerVersion, enabled: false).RequiresUpdate("0.1.0").ShouldBeFalse();
    }

    /// <summary>
    /// Uyumsuz ajan: kayıt cevabı güncelleme ister, durum "bağlı değil + güncelleme gerekli", komut kanalı hiç çağrılmaz, "Ajanı test et"
    /// "Ajan güncellemesi gerekli" döner (AC-6); ajan güncellenip aynı sürümle yeniden kaydolunca hepsi normale döner.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Gate_MismatchedAgent_NoCommandsPingRefusedStatusFlag()
    {
        var presence = Presence();
        var commands = new AgentCommands(_registry, _channel);
        var ct = TestContext.Current.CancellationToken;

        var reply = await presence.RegisterAsync("c1", _owner, new AgentRegistration("PC", "0.4.0"), ct);

        reply.ShouldBe(new AgentRegisterReply(ServerVersion, true));
        var status = presence.GetStatus(_owner.UserId);
        status.IsConnected.ShouldBeFalse();
        status.UpdateRequired.ShouldBeTrue();
        status.AgentVersion.ShouldBe("0.4.0");
        commands.ConnectedDevice(_owner.UserId).ShouldBeNull();
        commands.UpdateRequired(_owner.UserId).ShouldBeTrue();
        (await commands.RunStageAsync(_owner.UserId, Stage(), ct)).ShouldBeNull();
        (await commands.CheckToolsAsync(_owner.UserId, new AgentToolCheckRequest([]), ct)).ShouldBeNull();
        (await commands.DeleteFilesAsync(_owner.UserId, new AgentDeleteFilesRequest(Guid.NewGuid(), ["a.mp4"]), ct)).ShouldBeNull();
        var test = await Tester().TestAsync(ct);
        test.Error.ShouldBe(AgentErrors.UpdateRequired);
        _channel.ReceivedCalls().ShouldBeEmpty();

        // Güncellendi: aynı sürümle yeniden kayıt.
        (await presence.RegisterAsync("c2", _owner, new AgentRegistration("PC", ServerVersion), ct)).UpdateRequired.ShouldBeFalse();
        presence.GetStatus(_owner.UserId).ShouldBe(new AgentStatusDto(true, "PC", ServerVersion, _time.GetUtcNow()));
        commands.ConnectedDevice(_owner.UserId).ShouldBe(_owner.DeviceId);
        commands.UpdateRequired(_owner.UserId).ShouldBeFalse();
    }

    /// <summary>
    /// "Üretiliyor" iş varken BeginUpdate reddedilir ve ajan komut almaya devam eder; iş yokken oturum "güncelleniyor" olur ve ajana
    /// (sıradaki işler dahil) komut gitmez (AC-3).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task BeginUpdate_ProducingRefused_ElseSessionUpdatingNoCommands()
    {
        var presence = Presence();
        var commands = new AgentCommands(_registry, _channel);
        var ct = TestContext.Current.CancellationToken;
        await presence.RegisterAsync("c1", _owner, new AgentRegistration("PC", ServerVersion), ct);

        _activity.IsProducingAsync(_owner.UserId, Arg.Any<CancellationToken>()).Returns(true);
        (await presence.BeginUpdateAsync("c1", _owner, ct)).ShouldBeFalse();
        commands.ConnectedDevice(_owner.UserId).ShouldBe(_owner.DeviceId);

        _activity.IsProducingAsync(_owner.UserId, Arg.Any<CancellationToken>()).Returns(false);
        (await presence.BeginUpdateAsync("other", _owner, ct)).ShouldBeFalse(); // başka bağlantı
        (await presence.BeginUpdateAsync("c1", new AgentOwner(Guid.NewGuid(), Guid.NewGuid()), ct)).ShouldBeFalse(); // başka sahip
        (await presence.BeginUpdateAsync("c1", _owner, ct)).ShouldBeTrue();

        commands.ConnectedDevice(_owner.UserId).ShouldBeNull();
        (await commands.RunStageAsync(_owner.UserId, Stage(), ct)).ShouldBeNull();
        await _channel.DidNotReceiveWithAnyArgs().RunStageAsync(default!, default!, ct);

        // Yeni sürüm yeniden bağlanınca oturum yenilenir, iş yeniden gönderilebilir.
        await presence.RegisterAsync("c2", _owner, new AgentRegistration("PC", ServerVersion), ct);
        commands.ConnectedDevice(_owner.UserId).ShouldBe(_owner.DeviceId);
    }

    /// <summary>
    /// Review r1 H-1, M-1: güncelleme kurulmadan biterse (EndUpdate) ajan yeniden komut alır; "Üretiliyor" denetimi sürerken oturum zaten
    /// "güncelleniyor"dur (yeni komut gitmez) ve iş bulunursa işaret geri alınır; başka bağlantı/sahip işareti değiştiremez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task EndUpdate_FailedUpdate_AcceptsCommandsAgain_BeginMarksBeforeCheck()
    {
        var presence = Presence();
        var commands = new AgentCommands(_registry, _channel);
        var ct = TestContext.Current.CancellationToken;
        await presence.RegisterAsync("c1", _owner, new AgentRegistration("PC", ServerVersion), ct);
        Guid? deviceDuringCheck = _owner.DeviceId;
        _activity.IsProducingAsync(_owner.UserId, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            deviceDuringCheck = commands.ConnectedDevice(_owner.UserId);
            return true;
        });

        (await presence.BeginUpdateAsync("c1", _owner, ct)).ShouldBeFalse();
        deviceDuringCheck.ShouldBeNull(); // denetim sırasında yeni komut kesik
        commands.ConnectedDevice(_owner.UserId).ShouldBe(_owner.DeviceId); // iş var → işaret geri alındı

        _activity.IsProducingAsync(_owner.UserId, Arg.Any<CancellationToken>()).Returns(false);
        (await presence.BeginUpdateAsync("c1", _owner, ct)).ShouldBeTrue();
        commands.ConnectedDevice(_owner.UserId).ShouldBeNull();
        presence.EndUpdate("c1", new AgentOwner(Guid.NewGuid(), Guid.NewGuid()));
        commands.ConnectedDevice(_owner.UserId).ShouldBeNull();
        presence.EndUpdate("c1", _owner);
        commands.ConnectedDevice(_owner.UserId).ShouldBe(_owner.DeviceId);
    }

    private static AgentStageCommand Stage() => new() { JobId = Guid.NewGuid(), Stage = AgentStageCommand.Voice, Engine = "hyperframes", Language = "tr", Format = "landscape" };

    private AgentPresenceService Presence() => new(
        _registry,
        Substitute.For<ISystemEventWriter>(),
        _time,
        NullLogger<AgentPresenceService>.Instance,
        new InMemoryInstalledComponentStore(),
        new AgentVersionGate(ServerVersion),
        _activity);

    private AgentTestService Tester()
    {
        var user = Substitute.For<ICurrentUser>();
        user.UserId.Returns(_owner.UserId);
        return new AgentTestService(_registry, _channel, _time, user);
    }
}
