using System.Diagnostics;
using System.Text;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;

namespace Konsept.Agent.Infrastructure.Base;

/// <summary>
/// Dış süreç çalıştırma (spec 0028 Req 10, 11): pencere açmadan, standart çıktı ve hata birlikte okunur, son <see cref="OutputLimit"/> karakter
/// saklanır. Süre sınırı aşılırsa ya da Durdur gelirse süreç ağacıyla öldürülür (Durdur'da <see cref="OperationCanceledException"/>).
/// Başlatılamayan süreç çıkış kodu -1 döner.
/// </summary>
public abstract class ProcessRunnerBase : IProcessRunner
{
    /// <summary>Saklanan çıktı uzunluğu.</summary>
    public const int OutputLimit = 8000;

    /// <inheritdoc />
    public async Task<ProcessResult> RunAsync(ProcessSpec spec, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var info = new ProcessStartInfo(spec.Executable)
        {
            WorkingDirectory = spec.WorkingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var arg in spec.Arguments)
        {
            info.ArgumentList.Add(arg);
        }

        foreach (var (key, value) in spec.Environment)
        {
            info.Environment[key] = value;
        }

        var output = new StringBuilder();
        void Append(string? line)
        {
            if (line is null)
            {
                return;
            }

            lock (output)
            {
                output.Append(line).Append('\n');
                if (output.Length > OutputLimit * 2)
                {
                    output.Remove(0, output.Length - OutputLimit);
                }
            }
        }

        Process process;
        try
        {
            process = Process.Start(info) ?? throw new InvalidOperationException("Süreç başlatılamadı.");
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            return new ProcessResult(-1, ex.Message);
        }

        using (process)
        {
            process.OutputDataReceived += (_, e) => Append(e.Data);
            process.ErrorDataReceived += (_, e) => Append(e.Data);
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(spec.Timeout);
            try
            {
                await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Kill(process);
                cancellationToken.ThrowIfCancellationRequested();
                return new ProcessResult(-1, Tail(output), TimedOut: true);
            }

            process.WaitForExit(); // Asenkron okuma tamamlansın.
            return new ProcessResult(process.ExitCode, Tail(output));
        }
    }

    private static void Kill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(5000); // Öldürülen sürecin tutamaçları (çalışma klasörü) bırakılsın.
        }
        catch (InvalidOperationException)
        {
            // Süreç zaten bitti.
        }
    }

    private static string Tail(StringBuilder output)
    {
        lock (output)
        {
            var text = output.ToString();
            return text.Length > OutputLimit ? text[^OutputLimit..] : text;
        }
    }
}
