using Microsoft.EntityFrameworkCore;
using TahaKonsept.Modules.Agents.Application.Concrete;
using TahaKonsept.Modules.Agents.Application.Options;
using TahaKonsept.Modules.Agents.Infrastructure.Persistence;
using TahaKonsept.Modules.Agents.Infrastructure.Persistence.Concrete;

namespace TahaKonsept.Modules.Agents.Tests.Support;

/// <summary>
/// Agents servislerini EF InMemory üzerinde kuran test düzeneği: saat, kullanıcı, depolar, izleyici, önizleme eşleyici,
/// sahte token üretici ve istemci sağlayıcı. Servisler <see cref="CreatePairingService"/>, <see cref="CreateHubService"/> vb. ile üretilir.
/// </summary>
public sealed class AgentsTestHarness : IDisposable
{
    private readonly DbContextOptions<AgentsDbContext> _options;
    private readonly List<AgentsDbContext> _extraContexts = [];

    /// <summary>Kurar.</summary>
    public AgentsTestHarness()
    {
        _options = new DbContextOptionsBuilder<AgentsDbContext>()
            .UseInMemoryDatabase("agents-" + Guid.NewGuid().ToString("N"))
            .Options;
        Db = new AgentsDbContext(_options, Clock);
        Devices = new AgentDeviceRepository(Db);
        Pairings = new DevicePairingRepository(Db);
        User.UserId = UserId;
    }

    /// <summary>Test kullanıcısının kimliği.</summary>
    public Guid UserId { get; } = Guid.CreateVersion7();

    /// <summary>Saat.</summary>
    public FakeClock Clock { get; } = new();

    /// <summary>Oturum kullanıcısı.</summary>
    public TestCurrentUser User { get; } = new();

    /// <summary>Ayarlar.</summary>
    public AgentsOptions Options { get; } = new() { PairingLifetimeSeconds = 600, PollIntervalSeconds = 5, SpeechPreviewTimeoutSeconds = 2 };

    /// <summary>DbContext.</summary>
    public AgentsDbContext Db { get; }

    /// <summary>Cihaz deposu.</summary>
    public AgentDeviceRepository Devices { get; }

    /// <summary>Eşleme deposu.</summary>
    public DevicePairingRepository Pairings { get; }

    /// <summary>Bağlantı izleyici.</summary>
    public InMemoryAgentConnectionTracker Tracker { get; } = new();

    /// <summary>Önizleme eşleyici.</summary>
    public SpeechPreviewCorrelator Previews { get; } = new();

    /// <summary>Token üretici.</summary>
    public FakeAgentTokenIssuer Tokens { get; } = new();

    /// <summary>İstemci sağlayıcı.</summary>
    public FakeAgentClientProvider Clients { get; } = new();

    /// <summary>Kod üretici.</summary>
    public RandomPairingCodeGenerator Codes { get; } = new();

    /// <summary>Eşleme servisi.</summary>
    /// <returns>Servis.</returns>
    public DevicePairingService CreatePairingService() =>
        new(Pairings, Devices, Codes, Tokens, new StaticVerificationUrlProvider(), User, Options, Clock);

    /// <summary>Aynı veritabanında ayrı DbContext'li ikinci eşleme servisi (eşzamanlı istek benzetimi).</summary>
    /// <returns>Servis ve onun eşleme deposu.</returns>
    public (DevicePairingService Service, DevicePairingRepository Pairings) CreateIsolatedPairingService()
    {
        var db = new AgentsDbContext(_options, Clock);
        _extraContexts.Add(db);
        var pairings = new DevicePairingRepository(db);
        return (new(pairings, new AgentDeviceRepository(db), Codes, Tokens, new StaticVerificationUrlProvider(), User, Options, Clock), pairings);
    }

    /// <summary>Hub servisi.</summary>
    /// <returns>Servis.</returns>
    public AgentHubService CreateHubService() =>
        new(Devices, Tracker, new NullAgentStageEventHandler(), new Infrastructure.Logging.Concrete.LoggingAgentLogSink(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Infrastructure.Logging.Concrete.LoggingAgentLogSink>.Instance),
            Previews, new FakeSecretProtector(), Options, Clock);

    /// <summary>Ajan kapısı.</summary>
    /// <returns>Kapı.</returns>
    public AgentGateway CreateGateway() => new(Tracker, Clients, Devices, Previews, new AgentVideoUrlBuilder(), Options);

    /// <summary>Cihaz servisi.</summary>
    /// <returns>Servis.</returns>
    public AgentDeviceService CreateDeviceService() =>
        new(Devices, Tracker, Tokens, new AgentSessionRevoker(Devices, Tracker, Clock), User, Clock);

    /// <summary>Kaynakları bırakır.</summary>
    public void Dispose()
    {
        foreach (var extra in _extraContexts)
        {
            extra.Dispose();
        }

        Db.Dispose();
    }
}
