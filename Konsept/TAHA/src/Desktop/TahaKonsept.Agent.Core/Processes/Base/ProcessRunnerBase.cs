using System.Diagnostics;
using System.Text;
using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;

namespace TahaKonsept.Agent.Core.Processes.Base;

/// <summary>
/// Süreç çalıştırıcı tabanı: stdout/stderr satır satır okunur ve geri çağrıya iletilir, stdin (varsa) yazılıp kapatılır,
/// zaman aşımında ya da iptalde <b>süreç ağacı</b> öldürülür. Zaman aşımı <see cref="ProcessResult.TimedOut"/> ile döner;
/// kullanıcı iptali <see cref="OperationCanceledException"/> fırlatır. Genişletme: <see cref="CreateStartInfo"/>, <see cref="OnStarted"/>, <see cref="OnCompleted"/>.
/// </summary>
public abstract class ProcessRunnerBase : IProcessRunner
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
    private static readonly TimeSpan StreamDrainTimeout = TimeSpan.FromSeconds(5);

    /// <inheritdoc />
    public async Task<ProcessResult> RunAsync(ProcessSpec spec, Action<string>? onOutput, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ct.ThrowIfCancellationRequested();

        using var process = new Process { StartInfo = CreateStartInfo(spec), EnableRaisingEvents = true };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var gate = new object();
        var stdoutClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stderrClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        process.OutputDataReceived += (_, e) => HandleLine(e.Data, stdout, gate, onOutput, stdoutClosed);
        process.ErrorDataReceived += (_, e) => HandleLine(e.Data, stderr, gate, onOutput, stderrClosed);

        var stopwatch = Stopwatch.StartNew();
        if (!process.Start())
        {
            throw new InvalidOperationException("Süreç başlatılamadı: " + spec.FileName);
        }

        OnStarted(spec, process.Id);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await WriteStandardInputAsync(process, spec.StandardInput);

        using var timeoutCts = spec.Timeout is { } timeout ? new CancellationTokenSource(timeout) : new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            KillTree(process);
            await WaitForExitAfterKillAsync(process);
            if (ct.IsCancellationRequested)
            {
                OnCompleted(spec, -1, stopwatch.Elapsed, cancelled: true);
                throw new OperationCanceledException("Süreç iptal edildi: " + spec.FileName, ct);
            }

            timedOut = true;
        }

        await Task.WhenAny(Task.WhenAll(stdoutClosed.Task, stderrClosed.Task), Task.Delay(StreamDrainTimeout, CancellationToken.None));
        stopwatch.Stop();

        var exitCode = timedOut ? -1 : SafeExitCode(process);
        OnCompleted(spec, exitCode, stopwatch.Elapsed, cancelled: false);
        string outText;
        string errText;
        lock (gate)
        {
            outText = stdout.ToString();
            errText = stderr.ToString();
        }

        return new ProcessResult(exitCode, outText, errText, stopwatch.Elapsed, timedOut);
    }

    /// <summary>Başlatma bilgisini üretir (yönlendirmeler açık, pencere yok, UTF-8).</summary>
    /// <param name="spec">Süreç tanımı.</param>
    /// <returns>Başlatma bilgisi.</returns>
    protected virtual ProcessStartInfo CreateStartInfo(ProcessSpec spec)
    {
        var info = new ProcessStartInfo(spec.FileName)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            StandardOutputEncoding = Utf8NoBom,
            StandardErrorEncoding = Utf8NoBom,
            StandardInputEncoding = Utf8NoBom,
        };

        foreach (var argument in spec.Arguments)
        {
            info.ArgumentList.Add(argument);
        }

        if (!string.IsNullOrWhiteSpace(spec.WorkingDirectory))
        {
            info.WorkingDirectory = spec.WorkingDirectory;
        }

        if (spec.Environment is not null)
        {
            foreach (var (key, value) in spec.Environment)
            {
                info.Environment[key] = value;
            }
        }

        return info;
    }

    /// <summary>Süreç başladığında çağrılır (ör. loglama).</summary>
    /// <param name="spec">Süreç tanımı.</param>
    /// <param name="processId">Süreç kimliği.</param>
    protected virtual void OnStarted(ProcessSpec spec, int processId)
    {
    }

    /// <summary>Süreç bittiğinde çağrılır.</summary>
    /// <param name="spec">Süreç tanımı.</param>
    /// <param name="exitCode">Çıkış kodu (-1: zaman aşımı/iptal).</param>
    /// <param name="duration">Süre.</param>
    /// <param name="cancelled">Kullanıcı iptali mi.</param>
    protected virtual void OnCompleted(ProcessSpec spec, int exitCode, TimeSpan duration, bool cancelled)
    {
    }

    /// <summary>Satır geri çağrısında oluşan hatayı bildirir (varsayılan: yut).</summary>
    /// <param name="exception">Hata.</param>
    protected virtual void OnOutputCallbackFailed(Exception exception)
    {
    }

    private void HandleLine(string? line, StringBuilder buffer, object gate, Action<string>? onOutput, TaskCompletionSource closed)
    {
        if (line is null)
        {
            closed.TrySetResult();
            return;
        }

        lock (gate)
        {
            buffer.AppendLine(line);
        }

        if (onOutput is null)
        {
            return;
        }

        try
        {
            onOutput(line);
        }
        catch (Exception ex)
        {
            OnOutputCallbackFailed(ex);
        }
    }

    private static async Task WriteStandardInputAsync(Process process, string? input)
    {
        try
        {
            if (!string.IsNullOrEmpty(input))
            {
                await process.StandardInput.WriteAsync(input);
                await process.StandardInput.FlushAsync();
            }

            process.StandardInput.Close();
        }
        catch (IOException)
        {
            // Süreç girişi okumadan kapandı; çıkış kodu sonucu belirler.
        }
    }

    private static void KillTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // Zaten kapanmış.
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Erişim reddedildi ya da kapanıyor.
        }
    }

    private static async Task WaitForExitAfterKillAsync(Process process)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Öldürülen süreç makul sürede kapanmadı; devam.
        }
    }

    private static int SafeExitCode(Process process)
    {
        try
        {
            return process.ExitCode;
        }
        catch (InvalidOperationException)
        {
            return -1;
        }
    }
}
