using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Engines.Speech.Python.Base;
using TahaKonsept.Agent.Engines.Speech.Python.Protocol;

namespace TahaKonsept.Agent.Engines.Speech.Python.Concrete;

/// <summary>
/// <c>tts_worker.py</c>'yi venv Python'u ile ayrı süreç olarak başlatır (<see cref="Start"/>); stdin/stdout UTF-8 protokol kanalı,
/// stderr satırları loga akar (son 20 satır hata mesajında kullanılır; <c>[taha:warn]</c> satırları <c>Warning</c>, diğerleri <c>Debug</c>). Kapatırken <c>quit</c> yerine süreç ağacını öldürür.
/// </summary>
public sealed class ProcessTtsWorkerClient : TtsWorkerClientBase
{
    private const int StderrTailLines = 20;

    private readonly Process _process;
    private readonly Queue<string> _stderrTail = new();
    private readonly Lock _stderrLock = new();

    private ProcessTtsWorkerClient(Process process, ILogger logger)
        : base(logger)
    {
        _process = process;
    }

    /// <inheritdoc />
    public override bool IsRunning
    {
        get
        {
            try
            {
                return !_process.HasExited;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }

    /// <inheritdoc />
    protected override TextWriter Input => _process.StandardInput;

    /// <inheritdoc />
    protected override TextReader Output => _process.StandardOutput;

    /// <summary>İşçiyi başlatır.</summary>
    /// <param name="pythonPath">venv python.exe.</param>
    /// <param name="scriptPath">tts_worker.py.</param>
    /// <param name="environment">Ek ortam değişkenleri (HF_HOME vb.).</param>
    /// <param name="logger">Logger.</param>
    /// <returns>Çalışan istemci.</returns>
    public static ProcessTtsWorkerClient Start(string pythonPath, string scriptPath, IReadOnlyDictionary<string, string> environment, ILogger logger)
    {
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        var startInfo = new ProcessStartInfo
        {
            FileName = pythonPath,
            WorkingDirectory = Path.GetDirectoryName(scriptPath)!,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = utf8,
            StandardOutputEncoding = utf8,
            StandardErrorEncoding = utf8,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("-u");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.Environment["PYTHONIOENCODING"] = "utf-8";
        startInfo.Environment["PYTHONUTF8"] = "1";
        startInfo.Environment["PYTHONDONTWRITEBYTECODE"] = "1";
        foreach (var (key, value) in environment)
        {
            startInfo.Environment[key] = value;
        }

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        var client = new ProcessTtsWorkerClient(process, logger);
        process.ErrorDataReceived += (_, e) => client.OnStderr(e.Data);
        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException($"Python işçisi başlatılamadı: {pythonPath}");
        }

        process.BeginErrorReadLine();
        logger.LogInformation("tts_worker başlatıldı (pid {Pid})", process.Id);
        return client;
    }

    /// <inheritdoc />
    protected override string DescribeExit()
    {
        string tail;
        lock (_stderrLock)
        {
            tail = string.Join(Environment.NewLine, _stderrTail);
        }

        var code = IsRunning ? "?" : SafeExitCode();
        return $"işçi kapandı (çıkış kodu {code}). {tail}".Trim();
    }

    /// <inheritdoc />
    protected override Task OnProtocolBrokenAsync()
    {
        Kill();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeCoreAsync()
    {
        if (IsRunning)
        {
            try
            {
                _process.StandardInput.Close();
                using var wait = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await _process.WaitForExitAsync(wait.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is OperationCanceledException or IOException or InvalidOperationException)
            {
                Kill();
            }
        }

        _process.Dispose();
    }

    private void OnStderr(string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (_stderrLock)
        {
            _stderrTail.Enqueue(line);
            while (_stderrTail.Count > StderrTailLines)
            {
                _stderrTail.Dequeue();
            }
        }

        if (WorkerProtocol.IsWarningLine(line))
        {
            // Adapter uyarıları (ör. "cuda istendi ama torch GPU görmüyor, CPU'ya düşülüyor") sessiz kalmasın.
            Logger.LogWarning("tts_worker: {Line}", line.Trim());
            return;
        }

        Logger.LogDebug("tts_worker stderr: {Line}", line);
    }

    private void Kill()
    {
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
        {
            Logger.LogDebug(ex, "tts_worker zaten kapanmış");
        }
    }

    private string SafeExitCode()
    {
        try
        {
            return _process.ExitCode.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (InvalidOperationException)
        {
            return "?";
        }
    }
}
