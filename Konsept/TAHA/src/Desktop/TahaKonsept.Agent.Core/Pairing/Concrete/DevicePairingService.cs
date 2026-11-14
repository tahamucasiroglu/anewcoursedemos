using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Core.Auth.Abstract;
using TahaKonsept.Agent.Core.Configuration;
using TahaKonsept.Agent.Core.Http.Abstract;
using TahaKonsept.Agent.Core.Pairing.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Agent.Pairing;

namespace TahaKonsept.Agent.Core.Pairing.Concrete;

/// <summary>Cihaz eşleme servisi: makine adı ve ajan sürümüyle başlatır, olayları loglar.</summary>
public sealed class DevicePairingService : DevicePairingServiceBase
{
    private readonly AgentOptions _options;
    private readonly ILogger<DevicePairingService> _logger;

    /// <summary>Oluşturur.</summary>
    /// <param name="api">Anonim API istemcisi.</param>
    /// <param name="auth">Oturum servisi.</param>
    /// <param name="options">Seçenekler.</param>
    /// <param name="timeProvider">Zaman sağlayıcı.</param>
    /// <param name="logger">Logger.</param>
    public DevicePairingService(IAnonymousAgentApiClient api, IAgentAuthService auth, AgentOptions options, TimeProvider timeProvider, ILogger<DevicePairingService> logger)
        : base(api, auth, timeProvider, options.PairingSlowDownStep)
    {
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override DeviceStartRequest CreateStartRequest() => new()
    {
        MachineName = Environment.MachineName,
        AgentVersion = _options.AgentVersion,
    };

    /// <inheritdoc />
    protected override void OnPaired() => _logger.LogInformation("Cihaz eşleme tamamlandı.");

    /// <inheritdoc />
    protected override void OnTransientPollError(Error error) =>
        _logger.LogWarning("Eşleme yoklaması geçici hata verdi ({Code}); devam ediliyor.", error.Code);
}
