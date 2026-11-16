using Konsept.Api.Tests.Fakes;

namespace Konsept.Api.Tests.Support;

/// <summary>
/// <see cref="KestrelApiFactory"/> örnekleri arasında paylaşılabilen bellek depoları ve kimlik (plan 0012 test kurulumu): aynı
/// portta yeniden açılan API, eşleşmiş ajanın belirtecini ve durumu soran kullanıcının oturumunu tanısın ("API yeniden başladı").
/// </summary>
public sealed class KestrelApiStores
{
    /// <summary>Initializes a new instance of the <see cref="KestrelApiStores"/> class.</summary>
    public KestrelApiStores()
    {
        Sessions = new InMemorySessionRepository(Users);
    }

    /// <summary>Gets kullanıcı deposu.</summary>
    public InMemoryUserRepository Users { get; } = new();

    /// <summary>Gets oturum deposu.</summary>
    public InMemorySessionRepository Sessions { get; }

    /// <summary>Gets ajan cihaz deposu.</summary>
    public InMemoryAgentDeviceRepository AgentDevices { get; } = new();

    /// <summary>Gets kimlik kilidi.</summary>
    public Lock Gate { get; } = new();

    /// <summary>Gets or sets fabrikaların istemcilerine eklenen Bearer token (bir kez alınır).</summary>
    public string? AccessToken { get; set; }
}
