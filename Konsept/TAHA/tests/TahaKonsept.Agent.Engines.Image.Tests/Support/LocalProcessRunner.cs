using System.Diagnostics;
using System.Text;
using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;

namespace TahaKonsept.Agent.Engines.Image.Tests.Support;

/// <summary>Gerçek üretim testi için en basit <see cref="IProcessRunner"/> (ajan çekirdeğinin çalıştırıcısına bağımlı olmamak için).</summary>
public sealed class LocalProcessRunner : IProcessRunner
{
    /// <inheritdoc />
    public async Task<ProcessResult> RunAsync(ProcessSpec spec, Action<string>? onOutput, CancellationToken ct)
    {
        var info = new ProcessStartInfo(spec.FileName)
        {
            WorkingDirectory = spec.WorkingDirectory ?? Environment.CurrentDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in spec.Arguments)
        {
            info.ArgumentList.Add(argument);
        }

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        using var process = new Process { StartInfo = info };
        process.OutputDataReceived += (_, e) => Append(stdout, e.Data, onOutput);
        process.ErrorDataReceived += (_, e) => Append(stderr, e.Data, onOutput);
        var watch = Stopwatch.StartNew();
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (spec.Timeout is { } limit)
        {
            timeout.CancelAfter(limit);
        }

        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            timedOut = !ct.IsCancellationRequested;
            if (!timedOut)
            {
                throw;
            }
        }

        return new ProcessResult(timedOut ? -1 : process.ExitCode, stdout.ToString(), stderr.ToString(), watch.Elapsed, timedOut);
    }

    private static void Append(StringBuilder target, string? line, Action<string>? onOutput)
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
