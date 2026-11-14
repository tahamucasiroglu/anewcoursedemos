using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Application.Concrete;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Contracts.Enums;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Konsept.Modules.Monitoring.Tests;

/// <summary>
/// Olay servisinin yazma/okuma davranışı.
/// </summary>
public sealed class SystemEventServiceTests
{
    private readonly ISystemEventRepository _repository = Substitute.For<ISystemEventRepository>();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 14, 9, 30, 0, TimeSpan.Zero));

    /// <summary>Yazılan olay saati TimeProvider'dan alır ve repository'ye kaydedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task WriteAsync_Entry_PersistsWithProviderTime()
    {
        var service = new SystemEventService(_repository, _time);

        var dto = await service.WriteAsync(
            new SystemEventEntry("agent.connected", SystemEventLevel.Info, "Ajan bağlandı"),
            TestContext.Current.CancellationToken);

        dto.OccurredAt.ShouldBe(_time.GetUtcNow());
        dto.Level.ShouldBe(SystemEventLevel.Info);
        await _repository.Received(1).AddAsync(
            Arg.Is<SystemEvent>(e => e.Type == "agent.connected" && e.OccurredAt == _time.GetUtcNow()),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Kayıt yokken son olay <c>null</c> döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task GetLatestAsync_NoEvents_ReturnsNull()
    {
        var service = new SystemEventService(_repository, _time);

        var latest = await service.GetLatestAsync(TestContext.Current.CancellationToken);

        latest.ShouldBeNull();
    }

    /// <summary>Son olay Contracts biçimine seviyesi korunarak çevrilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task GetLatestAsync_EventExists_MapsLevelAndMessage()
    {
        var stored = SystemEvent.Create("agent.disconnected", EventLevel.Warning, "Ajan bağlantısı koptu", _time.GetUtcNow(), null);
        _repository.GetLatestAsync(Arg.Any<CancellationToken>()).Returns(stored);
        var service = new SystemEventService(_repository, _time);

        var latest = await service.GetLatestAsync(TestContext.Current.CancellationToken);

        latest.ShouldNotBeNull();
        latest.Level.ShouldBe(SystemEventLevel.Warning);
        latest.Message.ShouldBe("Ajan bağlantısı koptu");
    }
}
