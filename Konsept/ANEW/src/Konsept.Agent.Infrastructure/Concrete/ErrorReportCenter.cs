using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Application.Base;
using Konsept.Agent.Domain.Entities;
using Serilog;

namespace Konsept.Agent.Infrastructure.Concrete;

/// <summary>Ajan hata bildirimi merkezi: teknik log Serilog'a, rapor dosyası diske, saat <see cref="TimeProvider"/>'dan.</summary>
/// <param name="connection">Sunucu bağlantısı.</param>
/// <param name="queue">Bekleyen bildirim deposu.</param>
/// <param name="logs">Son log satırları.</param>
/// <param name="info">Ajan bilgisi.</param>
/// <param name="environment">Ortam.</param>
/// <param name="timeProvider">Saat kaynağı.</param>
/// <param name="logger">Teknik log; verilmezse yazılmaz.</param>
public sealed class ErrorReportCenter(
    IServerConnection connection,
    IErrorReportQueue queue,
    IRecentLogSource logs,
    IAgentInfoProvider info,
    ReportEnvironment environment,
    TimeProvider timeProvider,
    ILogger? logger = null) : ErrorReportCenterBase(connection, queue, logs, info, environment)
{
    private readonly ILogger _logger = (logger ?? Serilog.Core.Logger.None).ForContext<ErrorReportCenter>();

    /// <inheritdoc />
    protected override void WriteLog(bool warning, string message)
    {
        if (warning)
        {
            _logger.Warning("{Message}", message);
        }
        else
        {
            _logger.Information("{Message}", message);
        }
    }

    /// <inheritdoc />
    protected override Task WriteFileAsync(string path, string text, CancellationToken cancellationToken) =>
        File.WriteAllTextAsync(path, text, cancellationToken);

    /// <inheritdoc />
    protected override DateTimeOffset UtcNow() => timeProvider.GetUtcNow();
}
