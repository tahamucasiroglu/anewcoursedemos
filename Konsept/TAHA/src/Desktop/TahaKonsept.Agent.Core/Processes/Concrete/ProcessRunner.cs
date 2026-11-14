using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Core.Processes.Base;

namespace TahaKonsept.Agent.Core.Processes.Concrete;

/// <summary>
/// Loglayan süreç çalıştırıcı: başlangıç (dosya, pid), bitiş (çıkış kodu, süre) ve iptal bilgilerini yazar.
/// </summary>
public sealed class ProcessRunner : ProcessRunnerBase
{
    private readonly ILogger<ProcessRunner> _logger;

    /// <summary>Oluşturur.</summary>
    /// <param name="logger">Logger.</param>
    public ProcessRunner(ILogger<ProcessRunner> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    protected override void OnStarted(ProcessSpec spec, int processId) =>
        _logger.LogInformation("Süreç başladı: {FileName} (pid {ProcessId})", Path.GetFileName(spec.FileName), processId);

    /// <inheritdoc />
    protected override void OnCompleted(ProcessSpec spec, int exitCode, TimeSpan duration, bool cancelled)
    {
        if (cancelled)
        {
            _logger.LogWarning("Süreç iptal edildi: {FileName} ({Duration} ms)", Path.GetFileName(spec.FileName), (long)duration.TotalMilliseconds);
            return;
        }

        var level = exitCode == 0 ? LogLevel.Information : LogLevel.Warning;
        _logger.Log(level, "Süreç bitti: {FileName} çıkış {ExitCode} ({Duration} ms)", Path.GetFileName(spec.FileName), exitCode, (long)duration.TotalMilliseconds);
    }

    /// <inheritdoc />
    protected override void OnOutputCallbackFailed(Exception exception) =>
        _logger.LogWarning(exception, "Süreç çıktı geri çağrısı hata verdi.");
}
