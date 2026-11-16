using System.Diagnostics;

namespace Konsept.Agent.Infrastructure.Sandbox;

/// <summary>
/// Kısıtlı süreç koşucusu (spec 0018 Req 1, L2 + L4): ortam sıfırdan kurulur (ajanın ortamı, sırlar, kullanıcı klasörleri geçmez), süreç
/// başlar başlamaz Job Object'e eklenir (alt süreçleri de içinde doğar), duvar saati süresi dolunca, Durdur'da ya da süreç bittiğinde Job'daki
/// her süreç sonlandırılır — geride render süreci kalmaz.
/// </summary>
public static class SandboxRunner
{
    private const int OutputTail = 8000;

    /// <summary>Süreci kısıtlı çalıştırır. Durdur'da ağaç sonlandırılır ve <see cref="OperationCanceledException"/> atılır.</summary>
    /// <param name="start">İstek.</param>
    /// <param name="cancellationToken">Durdur belirteci.</param>
    /// <returns>Sonuç.</returns>
    public static async Task<SandboxRun> RunAsync(SandboxStart start, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(start);
        var info = new ProcessStartInfo(start.Executable)
        {
            WorkingDirectory = start.WorkingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in start.Arguments)
        {
            info.ArgumentList.Add(arg);
        }

        info.Environment.Clear();
        foreach (var (key, value) in start.Environment)
        {
            info.Environment[key] = value;
        }

        using var job = RenderJobObject.Create(start.Limits);
        using var process = Process.Start(info) ?? throw new InvalidOperationException("Süreç başlatılamadı.");
        try
        {
            job.Assign(process);
        }
        catch
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        process.StandardInput.Close();
        var stdout = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var stderr = process.StandardError.ReadToEndAsync(CancellationToken.None);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(start.Limits.SceneTimeout);
        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            job.Terminate();
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            timedOut = true;
        }

        job.Terminate(); // Süreç bitti; arkada kalan alt süreç (tarayıcı, kodlayıcı) da ölür.
        await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
        var limit = job.LimitExceeded(failed: timedOut || process.ExitCode != 0);
        var output = await ReadAsync(stdout).ConfigureAwait(false) + "\n" + await ReadAsync(stderr).ConfigureAwait(false);
        return new SandboxRun(
            timedOut ? -1 : process.ExitCode,
            output.Length > OutputTail ? output[^OutputTail..] : output,
            timedOut || limit,
            job.ActiveProcesses);
    }

    private static async Task<string> ReadAsync(Task<string> read)
    {
        try
        {
            return await read.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            return string.Empty;
        }
    }
}
