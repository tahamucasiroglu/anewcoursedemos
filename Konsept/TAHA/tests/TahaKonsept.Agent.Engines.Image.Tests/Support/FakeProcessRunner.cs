using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;

namespace TahaKonsept.Agent.Engines.Image.Tests.Support;

/// <summary>Süreci çalıştırmayan sahte çalıştırıcı: tanımı kaydeder, isteğe göre <c>-o</c> dosyasını oluşturur ve çıkış kodunu döner.</summary>
public sealed class FakeProcessRunner : IProcessRunner
{
    /// <summary>Dönülecek çıkış kodu.</summary>
    public int ExitCode { get; init; }

    /// <summary>Başarılıysa <c>-o</c> ile verilen dosyayı oluştursun mu.</summary>
    public bool CreateOutput { get; init; } = true;

    /// <summary>Çıktı geri çağrısına gönderilecek satırlar.</summary>
    public IReadOnlyList<string> OutputLines { get; init; } = [];

    /// <summary>Son çalıştırılan tanım.</summary>
    public ProcessSpec? LastSpec { get; private set; }

    /// <inheritdoc />
    public Task<ProcessResult> RunAsync(ProcessSpec spec, Action<string>? onOutput, CancellationToken ct)
    {
        LastSpec = spec;
        foreach (var line in OutputLines)
        {
            onOutput?.Invoke(line);
        }

        var outIndex = spec.Arguments.ToList().IndexOf("-o");
        if (ExitCode == 0 && CreateOutput && outIndex >= 0)
        {
            File.WriteAllBytes(spec.Arguments[outIndex + 1], [0x89, 0x50, 0x4E, 0x47]);
        }

        return Task.FromResult(new ProcessResult(ExitCode, string.Join('\n', OutputLines), string.Empty, TimeSpan.FromSeconds(1), false));
    }
}
