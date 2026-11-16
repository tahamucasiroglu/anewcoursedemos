using Konsept.Modules.AgentGateway.Application.Concrete;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.AgentGateway.Domain.Entities;
using Konsept.Modules.AgentGateway.Infrastructure.Concrete;
using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Contracts.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Konsept.Modules.AgentGateway.Tests;

/// <summary>
/// Ajan bağlanma/kopma olayları ve durum (spec 0001 AC-6, AC-8).
/// </summary>
public sealed class AgentPresenceServiceTests
{
    private readonly InMemoryAgentConnectionRegistry _registry = new();
    private readonly ISystemEventWriter _events = Substitute.For<ISystemEventWriter>();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 14, 10, 0, 0, TimeSpan.Zero));

    // Test kurulumu (plan 0012): ajan oturumu sahibiyle kaydolur; durum sahibine göre okunur.
    private static readonly AgentOwner Owner = new(Guid.NewGuid(), Guid.NewGuid());

    /// <summary>Kayıt olunca durum "bağlı" olur ve "Ajan bağlandı" olayı yazılır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Register_Agent_MarksConnectedAndWritesEvent()
    {
        var service = new AgentPresenceService(_registry, _events, _time, NullLogger<AgentPresenceService>.Instance);

        await service.RegisterAsync("c1", Owner, new AgentRegistration("TAHA-PC", "0.1.0"), TestContext.Current.CancellationToken);

        var status = service.GetStatus(Owner.UserId);
        status.IsConnected.ShouldBeTrue();
        status.MachineName.ShouldBe("TAHA-PC");
        status.ConnectedAt.ShouldBe(_time.GetUtcNow());
        await _events.Received(1).WriteAsync(
            Arg.Is<SystemEventEntry>(e => e.Type == AgentPresenceService.ConnectedEventType
                && e.Level == SystemEventLevel.Info && e.Message.Contains("TAHA-PC", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Geçerli ajan koparsa durum "bağlı değil" olur ve uyarı olayı yazılır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Disconnect_CurrentAgent_MarksDisconnectedAndWritesWarning()
    {
        var service = new AgentPresenceService(_registry, _events, _time, NullLogger<AgentPresenceService>.Instance);
        await service.RegisterAsync("c1", Owner, new AgentRegistration("TAHA-PC", "0.1.0"), TestContext.Current.CancellationToken);

        await service.DisconnectAsync("c1", TestContext.Current.CancellationToken);

        service.GetStatus(Owner.UserId).IsConnected.ShouldBeFalse();
        await _events.Received(1).WriteAsync(
            Arg.Is<SystemEventEntry>(e => e.Type == AgentPresenceService.DisconnectedEventType && e.Level == SystemEventLevel.Warning),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Yeniden bağlanan ajanın eski bağlantısının geç gelen kopması yeni oturumu silmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Disconnect_StaleConnectionAfterReconnect_KeepsNewSession()
    {
        var service = new AgentPresenceService(_registry, _events, _time, NullLogger<AgentPresenceService>.Instance);
        await service.RegisterAsync("old", Owner, new AgentRegistration("TAHA-PC", "0.1.0"), TestContext.Current.CancellationToken);
        await service.RegisterAsync("new", Owner, new AgentRegistration("TAHA-PC", "0.1.0"), TestContext.Current.CancellationToken);

        await service.DisconnectAsync("old", TestContext.Current.CancellationToken);

        service.GetStatus(Owner.UserId).IsConnected.ShouldBeTrue();
        await _events.DidNotReceive().WriteAsync(
            Arg.Is<SystemEventEntry>(e => e.Type == AgentPresenceService.DisconnectedEventType),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Olay kaydına yazılamasa da (veritabanı kapalı) kayıt başarılı döner ve ajan "bağlı" görünür (review M-1).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Register_EventWriteFails_StillConnectedAndDoesNotThrow()
    {
        _events.WriteAsync(Arg.Any<SystemEventEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<SystemEventDto>(new InvalidOperationException("veritabanı kapalı")));
        var service = new AgentPresenceService(_registry, _events, _time, NullLogger<AgentPresenceService>.Instance);

        await Should.NotThrowAsync(() => service.RegisterAsync("c1", Owner, new AgentRegistration("TAHA-PC", "0.1.0"), TestContext.Current.CancellationToken));

        service.GetStatus(Owner.UserId).IsConnected.ShouldBeTrue();
        await Should.NotThrowAsync(() => service.DisconnectAsync("c1", TestContext.Current.CancellationToken));
        service.GetStatus(Owner.UserId).IsConnected.ShouldBeFalse();
    }
}
