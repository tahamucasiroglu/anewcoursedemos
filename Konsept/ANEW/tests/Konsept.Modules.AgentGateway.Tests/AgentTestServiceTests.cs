using Konsept.Modules.AgentGateway.Application;
using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Application.Concrete;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.AgentGateway.Domain.Entities;
using Konsept.Modules.AgentGateway.Infrastructure.Concrete;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Konsept.Modules.AgentGateway.Tests;

/// <summary>
/// "Ajanı test et" (spec 0001 AC-7): bağlı ajan cevabı, bağlı değil, 10 sn zaman aşımı.
/// </summary>
public sealed class AgentTestServiceTests
{
    private readonly InMemoryAgentConnectionRegistry _registry = new();
    private readonly IAgentChannel _channel = Substitute.For<IAgentChannel>();
    private readonly FakeTimeProvider _time = new();

    /// <summary>Bağlı ajan cevap verirse versiyonu ve makine adı döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task TestAgent_Connected_ReturnsVersionAndMachine()
    {
        _registry.Set(new AgentSession("c1", "TAHA-PC", "0.1.0", _time.GetUtcNow()));
        _channel.PingAsync("c1", Arg.Any<CancellationToken>()).Returns(new AgentPingReply("TAHA-PC", "0.1.0"));
        var service = new AgentTestService(_registry, _channel, _time);

        var result = await service.TestAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.MachineName.ShouldBe("TAHA-PC");
        result.Value.AgentVersion.ShouldBe("0.1.0");
    }

    /// <summary>Ajan bağlı değilken "Ajan bağlı değil" hatası döner, komut gönderilmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task TestAgent_NotConnected_ReturnsError()
    {
        var service = new AgentTestService(_registry, _channel, _time);

        var result = await service.TestAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(AgentErrors.NotConnected);
        result.Error!.Message.ShouldBe("Ajan bağlı değil");
        await _channel.DidNotReceiveWithAnyArgs().PingAsync(default!, TestContext.Current.CancellationToken);
    }

    /// <summary>Cevap 10 sn içinde gelmezse "Zaman aşımı" hatası döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task TestAgent_NoReply10s_ReturnsTimeout()
    {
        _registry.Set(new AgentSession("c1", "TAHA-PC", "0.1.0", _time.GetUtcNow()));
        _channel.PingAsync("c1", Arg.Any<CancellationToken>())
            .Returns(call => Task.Delay(Timeout.Infinite, call.Arg<CancellationToken>()).ContinueWith<AgentPingReply>(
                t => throw new OperationCanceledException(),
                TaskScheduler.Default));
        var service = new AgentTestService(_registry, _channel, _time);

        var pending = service.TestAsync(TestContext.Current.CancellationToken);
        _time.Advance(TimeSpan.FromSeconds(9));
        pending.IsCompleted.ShouldBeFalse();
        _time.Advance(TimeSpan.FromSeconds(1));
        var result = await pending;

        result.Error.ShouldBe(AgentErrors.Timeout);
        result.Error!.Message.ShouldBe("Zaman aşımı");
    }

    /// <summary>Gönderim sırasında bağlantı koparsa "Ajan bağlı değil" döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task TestAgent_ConnectionLostDuringSend_ReturnsNotConnected()
    {
        _registry.Set(new AgentSession("c1", "TAHA-PC", "0.1.0", _time.GetUtcNow()));
        _channel.PingAsync("c1", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<AgentPingReply>(new IOException("bağlantı yok")));
        var service = new AgentTestService(_registry, _channel, _time);

        var result = await service.TestAsync(TestContext.Current.CancellationToken);

        result.Error.ShouldBe(AgentErrors.NotConnected);
    }
}
