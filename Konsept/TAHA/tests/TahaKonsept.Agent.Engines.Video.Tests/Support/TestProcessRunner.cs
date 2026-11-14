using System.Diagnostics;
using System.Text;
using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;

namespace TahaKonsept.Agent.Engines.Video.Tests.Support;

/// <summary>Testler için basit gerçek süreç çalıştırıcı (ArgumentList, ortam birleştirme, satır geri çağrısı, zaman aşımı, ağaç öldürme).</summary>
public sealed class TestProcessRunner : IProcessRunner
{
    /// <inheritdoc />
    public async Task<ProcessResult> RunAsync(ProcessSpec spec, Action<string>? onOutput, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(spec.FileName)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = spec.StandardInput is not null,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var arg in spec.Arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        if (spec.WorkingDirectory is not null)
        {
            psi.WorkingDirectory = spec.WorkingDirectory;
        }

        if (spec.Environment is not null)
        {
            foreach (var (key, value) in spec.Environment)
            {
                psi.Environment[key] = value;
            }
        }

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var sw = Stopwatch.StartNew();
        using var process = new Process { StartInfo = psi };
        process.OutputDataReceived += (_, e) => Collect(e.Data, stdout, onOutput);
        process.ErrorDataReceived += (_, e) => Collect(e.Data, stderr, onOutput);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        if (spec.StandardInput is not null)
        {
            await process.StandardInput.WriteAsync(spec.StandardInput).ConfigureAwait(false);
            process.StandardInput.Close();
        }

        using var timeout = spec.Timeout is { } t ? new CancellationTokenSource(t) : new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);
        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            timedOut = timeout.IsCancellationRequested;
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
        }

        process.WaitForExit();
        lock (stdout)
        {
            lock (stderr)
            {
                return new ProcessResult(process.ExitCode, stdout.ToString(), stderr.ToString(), sw.Elapsed, timedOut);
            }
        }
    }

    private static void Collect(string? line, StringBuilder target, Action<string>? onOutput)
    {
        if (line is null)
        {
            return;
        }

        lock (target)
        {
            target.AppendLine(line);
        }

        onOutput?.Invoke(line);
    }
}
