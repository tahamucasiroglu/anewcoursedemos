using System.Diagnostics;
using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;

namespace TahaKonsept.Agent.Engines.Speech.Tests.Support;

/// <summary>Testler için basit <see cref="IProcessRunner"/>: süreci çalıştırır, stdout/stderr toplar, zaman aşımında öldürür.</summary>
public sealed class SimpleProcessRunner : IProcessRunner
{
    /// <inheritdoc />
    public async Task<ProcessResult> RunAsync(ProcessSpec spec, Action<string>? onOutput, CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo(spec.FileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = spec.WorkingDirectory ?? Environment.CurrentDirectory,
        };
        foreach (var argument in spec.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach (var (key, value) in spec.Environment ?? new Dictionary<string, string>())
        {
            startInfo.Environment[key] = value;
        }

        var stdout = new System.Text.StringBuilder();
        var stderr = new System.Text.StringBuilder();
        var watch = Stopwatch.StartNew();
        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stdout.AppendLine(e.Data);
                onOutput?.Invoke(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stderr.AppendLine(e.Data);
                onOutput?.Invoke(e.Data);
            }
        };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(spec.Timeout ?? Timeout.InfiniteTimeSpan);
        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            timedOut = !ct.IsCancellationRequested;
            process.Kill(entireProcessTree: true);
            if (!timedOut)
            {
                throw;
            }
        }

        return new ProcessResult(timedOut ? -1 : process.ExitCode, stdout.ToString(), stderr.ToString(), watch.Elapsed, timedOut);
    }
}
