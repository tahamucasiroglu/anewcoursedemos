using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Application.Base;
using Konsept.Modules.Identity.Contracts.Abstract;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Identity.Application.Concrete;

/// <summary>Cihaz iptali; dinleyici hatası Warning olarak loglanır.</summary>
/// <param name="devices">Cihaz deposu.</param>
/// <param name="listeners">İptal dinleyicileri.</param>
/// <param name="timeProvider">Saat kaynağı.</param>
/// <param name="logger">Teknik log.</param>
public sealed partial class AgentDeviceRevoker(
    IAgentDeviceRepository devices,
    IEnumerable<IAgentDeviceRevocationListener> listeners,
    TimeProvider timeProvider,
    ILogger<AgentDeviceRevoker> logger) : AgentDeviceRevokerBase(devices, listeners, timeProvider)
{
    /// <inheritdoc />
    protected override void OnListenerFailed(Exception exception) => LogListenerFailed(logger, exception);

    [LoggerMessage(EventId = 2202, Level = LogLevel.Warning, Message = "Ajan iptali bağlantıya bildirilemedi; cihaz yine de iptal edildi.")]
    private static partial void LogListenerFailed(ILogger logger, Exception exception);
}
