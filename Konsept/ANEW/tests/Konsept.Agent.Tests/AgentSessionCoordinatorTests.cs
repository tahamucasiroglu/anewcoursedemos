using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Application.Base;
using Konsept.Agent.Application.Concrete;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using NSubstitute;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// Ajanın oturum akışı, sahte sunucuyla (spec 0012 AC-1, AC-3, AC-4, AC-7, AC-8, AC-9 ajan tarafı): kod ve geri sayım,
/// saklı belirteçle eşleştirmesiz bağlanma, ret/süre dolumu ve yeni kod, oturum kapanınca belirtecin silinmesi ve yeni kod,
/// sunucuya ulaşılamazken yeniden deneme.
/// </summary>
public sealed class AgentSessionCoordinatorTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);
    private readonly IServerConnection _connection = Substitute.For<IServerConnection>();
    private readonly IPairingClient _pairing = Substitute.For<IPairingClient>();
    private readonly MemoryTokenStore _tokens = new();
    private readonly IAgentInfoProvider _info = Substitute.For<IAgentInfoProvider>();

    /// <summary>Initializes a new instance of the <see cref="AgentSessionCoordinatorTests"/> class.</summary>
    public AgentSessionCoordinatorTests()
    {
        _info.MachineName.Returns("TAHA-PC");
        _info.Version.Returns("0.5.0");
        _connection.State.Returns(ConnectionState.Disconnected);
        _pairing.CheckAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new PairingPoll(PairingPollState.Pending));
    }

    /// <summary>Eşleşmemiş ajan açılınca kodu ve 10 dakikalık son geçerliliği gösterir; sunucuya bağlanmaya çalışmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Start_NoToken_ShowsCodeAndCountdown()
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(10);
        _pairing.RequestCodeAsync("TAHA-PC", "0.5.0", Arg.Any<CancellationToken>()).Returns(new PairingCodeInfo("ABCD-2345", "knr_sir", expires));
        var session = Create();

        await session.StartAsync(TestContext.Current.CancellationToken);
        await WaitAsync(() => session.Current.Code is not null);

        session.Current.ShouldBe(new AgentSessionSnapshot(AgentSessionState.Unpaired, "ABCD-2345", expires));
        await _connection.DidNotReceive().StartAsync(Arg.Any<CancellationToken>());
        await session.StopAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>Onay gelince belirteç saklanır, bağlantı başlar ve bağlanınca "Eşleşti" görünür.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Approved_SavesTokenAndConnects()
    {
        _pairing.RequestCodeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PairingCodeInfo("ABCD-2345", "knr_sir", DateTimeOffset.UtcNow.AddMinutes(10)));
        _pairing.CheckAsync("knr_sir", Arg.Any<CancellationToken>()).Returns(new PairingPoll(PairingPollState.Approved, "knr_belirtec"));
        var session = Create();

        await session.StartAsync(TestContext.Current.CancellationToken);
        await WaitAsync(() => _tokens.Read() == "knr_belirtec");
        await WaitAsync(() => _connection.ReceivedCalls().Any(c => c.GetMethodInfo().Name == nameof(IServerConnection.StartAsync)));
        _connection.StateChanged += Raise.Event<EventHandler<ConnectionState>>(_connection, ConnectionState.Connected);

        session.Current.State.ShouldBe(AgentSessionState.Paired);
        await session.StopAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>Saklı belirteci olan ajan yeniden açılınca kod istemeden bağlanır (yeniden eşleştirme yok).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Start_StoredToken_ConnectsWithoutPairing()
    {
        _tokens.Save("knr_saklı");
        var session = Create();

        await session.StartAsync(TestContext.Current.CancellationToken);
        _connection.StateChanged += Raise.Event<EventHandler<ConnectionState>>(_connection, ConnectionState.Connected);

        session.Current.State.ShouldBe(AgentSessionState.Paired);
        await _connection.Received(1).StartAsync(Arg.Any<CancellationToken>());
        await _pairing.DidNotReceiveWithAnyArgs().RequestCodeAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Reddedilen kodda "reddedildi", süresi dolan kodda "süresi doldu" görünür; ikisinde de "Yeni kod al" yeni kod getirir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task CodeRejectedOrExpired_ShowsState_NewCodeOnRequest()
    {
        _pairing.RequestCodeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(
            new PairingCodeInfo("RRRR-2222", "knr_ret", DateTimeOffset.UtcNow.AddMinutes(10)),
            new PairingCodeInfo("SSSS-3333", "knr_kisa", DateTimeOffset.UtcNow.AddMilliseconds(300)),
            new PairingCodeInfo("TTTT-4444", "knr_yeni", DateTimeOffset.UtcNow.AddMinutes(10)));
        _pairing.CheckAsync("knr_ret", Arg.Any<CancellationToken>()).Returns(new PairingPoll(PairingPollState.Rejected));
        var session = Create();

        await session.StartAsync(TestContext.Current.CancellationToken);
        await WaitAsync(() => session.Current.State == AgentSessionState.Rejected);
        session.RequestNewCode();
        await WaitAsync(() => session.Current.State == AgentSessionState.CodeExpired);
        session.RequestNewCode();
        await WaitAsync(() => session.Current.Code == "TTTT-4444");

        session.Current.State.ShouldBe(AgentSessionState.Unpaired);
        await session.StopAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Sunucu oturumu kapatınca (çıkış ya da yeni eşleştirme) bağlantı askıya alınır, belirteç silinir, "Oturum kapandı" ile
    /// yeni kod gösterilir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task SessionRevoked_ClearsToken_ShowsClosedAndNewCode()
    {
        _tokens.Save("knr_eski");
        _pairing.RequestCodeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PairingCodeInfo("YENI-2345", "knr_sir", DateTimeOffset.UtcNow.AddMinutes(10)));
        var session = Create();
        await session.StartAsync(TestContext.Current.CancellationToken);
        _connection.StateChanged += Raise.Event<EventHandler<ConnectionState>>(_connection, ConnectionState.Connected);

        _connection.SessionRevoked += Raise.Event<EventHandler>(_connection, EventArgs.Empty);
        await WaitAsync(() => session.Current.Code == "YENI-2345");

        session.Current.State.ShouldBe(AgentSessionState.SessionClosed);
        _tokens.Read().ShouldBeNull();
        await _connection.Received(1).SuspendAsync(Arg.Any<CancellationToken>());
        await session.StopAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>Sunucuya ulaşılamazken "Sunucuya ulaşılamıyor" görünür ve kendiliğinden yeniden denenir; sunucu gelince kod gösterilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ServerUnreachable_ShowsUnreachable_RetriesUntilCode()
    {
        var states = new List<AgentSessionState>();
        _pairing.RequestCodeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(
            _ => Task.FromException<PairingCodeInfo>(new HttpRequestException("sunucu yok")),
            _ => Task.FromException<PairingCodeInfo>(new HttpRequestException("sunucu yok")),
            _ => Task.FromResult(new PairingCodeInfo("GELD-2345", "knr_sir", DateTimeOffset.UtcNow.AddMinutes(10))));
        var session = Create();
        session.Changed += (_, s) =>
        {
            lock (states)
            {
                states.Add(s.State);
            }
        };

        await session.StartAsync(TestContext.Current.CancellationToken);
        await WaitAsync(() => session.Current.Code == "GELD-2345");

        lock (states)
        {
            states.ShouldBe([AgentSessionState.Unreachable, AgentSessionState.Unpaired]);
        }

        await _pairing.Received(3).RequestCodeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await session.StopAsync(TestContext.Current.CancellationToken);
    }

    private static async Task WaitAsync(Func<bool> condition)
    {
        var until = DateTime.UtcNow + Limit;
        while (!condition())
        {
            if (DateTime.UtcNow > until)
            {
                throw new TimeoutException("Koşul süresinde gerçekleşmedi.");
            }

            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }

    private AgentSessionCoordinator Create() =>
        new(_connection, _pairing, _tokens, _info, new AgentSessionOptions(TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(20)), TimeProvider.System);
}
