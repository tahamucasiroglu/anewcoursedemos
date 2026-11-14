using Microsoft.Extensions.Logging;
using TahaKonsept.Modules.Agents.Application.Base;
using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Agents.Infrastructure.Logging.Concrete;

/// <summary>
/// Varsayılan ajan günlük alıcısı: kayıtları API'nin Serilog günlüğüne (seviye eşlemeli) yazar. Diagnostics modülü kullanıcıya görünen
/// günlüğe yazan kendi <c>IAgentLogSink</c>'ini kaydedince (Agents'tan önce ya da Replace ile) bu devre dışı kalır.
/// </summary>
public sealed partial class LoggingAgentLogSink : AgentLogSinkBase
{
    private readonly ILogger<LoggingAgentLogSink> _logger;

    /// <summary>Oluşturur.</summary>
    /// <param name="logger">Günlükçü.</param>
    public LoggingAgentLogSink(ILogger<LoggingAgentLogSink> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Kaydı yazar.</summary>
    /// <param name="agent">Ajan bağlamı.</param>
    /// <param name="entry">Kayıt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public override Task WriteAsync(AgentCallContext agent, AgentLogEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentNullException.ThrowIfNull(entry);
        var level = entry.Level switch
        {
            LogLevelKind.Debug => LogLevel.Debug,
            LogLevelKind.Warning => LogLevel.Warning,
            LogLevelKind.Error => LogLevel.Error,
            LogLevelKind.Critical => LogLevel.Critical,
            _ => LogLevel.Information,
        };
        LogAgentEntry(_logger, level, FormatSource(entry), agent.DeviceId, entry.JobId, entry.Message, entry.Exception);
        return Task.CompletedTask;
    }

    [LoggerMessage(EventId = 7301, Message = "[{Source}] cihaz={DeviceId} iş={JobId}: {AgentMessage} {AgentException}")]
    private static partial void LogAgentEntry(
        ILogger logger,
        LogLevel level,
        string source,
        Guid? deviceId,
        Guid? jobId,
        string agentMessage,
        string? agentException);
}
