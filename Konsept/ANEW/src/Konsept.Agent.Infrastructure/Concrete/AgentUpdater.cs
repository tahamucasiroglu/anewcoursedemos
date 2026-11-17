using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Application.Base;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Infrastructure.Logging;
using Serilog;

namespace Konsept.Agent.Infrastructure.Concrete;

/// <summary>
/// Ajan güncellemesi (spec 0023); davranış <see cref="AgentUpdaterBase"/>'dedir, burada yalnızca yerel teknik log (0008, Serilog). Güncelleme
/// hataları Error yazılır ama 0022 uyarısını kendiliğinden açmaz (<see cref="ErrorAlertSink.NoAlertProperty"/>); kullanıcı güncelleme ekranındaki
/// "Hatayı bildir" ile açar.
/// </summary>
/// <param name="currentVersion">Kurulu sürüm.</param>
/// <param name="feed">Güncelleme adresi.</param>
/// <param name="host">İndirme, SHA-256, disk.</param>
/// <param name="verifier">İmza doğrulayıcı.</param>
/// <param name="installation">Kurulu kopya.</param>
/// <param name="connection">Sunucu bağlantısı.</param>
/// <param name="time">Saat.</param>
/// <param name="logger">Teknik log.</param>
public sealed class AgentUpdater(
    string currentVersion,
    Uri feed,
    ISetupHost host,
    IManifestVerifier verifier,
    IAgentInstallation installation,
    IServerConnection connection,
    TimeProvider time,
    ILogger? logger = null) : AgentUpdaterBase(currentVersion, feed, host, verifier, installation, connection, time)
{
    private readonly ILogger _logger = (logger ?? Serilog.Core.Logger.None).ForContext<AgentUpdater>();

    /// <inheritdoc />
    protected override void WriteLog(AgentLogLevel level, string messageTemplate, params object?[] values)
    {
        switch (level)
        {
            case AgentLogLevel.Information:
                _logger.Information(messageTemplate, values);
                break;
            case AgentLogLevel.Warning:
                _logger.Warning(messageTemplate, values);
                break;
            default:
                _logger.ForContext(ErrorAlertSink.NoAlertProperty, true).Error(messageTemplate, values);
                break;
        }
    }
}
