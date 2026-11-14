using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;

namespace TahaKonsept.Agent.Engines.Image.Tests.Support;

/// <summary>
/// Sırayla senaryo dönen sahte çalıştırıcı: her çağrıda sıradaki (çıkış kodu, çıktı satırları) çiftini kullanır (biterse sonuncusu),
/// başarılıysa <c>-o</c> dosyasını oluşturur ve tüm tanımları <see cref="Specs"/>'te tutar (OOM sonrası yeniden deneme testleri için).
/// </summary>
public sealed class ScriptedProcessRunner : IProcessRunner
{
    private readonly Queue<(int ExitCode, string[] Lines)> _script;
    private (int ExitCode, string[] Lines) _last = (0, []);

    /// <summary>Oluşturur.</summary>
    /// <param name="script">Sıralı senaryo.</param>
    public ScriptedProcessRunner(params (int ExitCode, string[] Lines)[] script)
    {
        _script = new Queue<(int ExitCode, string[] Lines)>(script);
    }

    /// <summary>Çalıştırılan tanımlar.</summary>
    public List<ProcessSpec> Specs { get; } = [];

    /// <inheritdoc />
    public Task<ProcessResult> RunAsync(ProcessSpec spec, Action<string>? onOutput, CancellationToken ct)
    {
        Specs.Add(spec);
        if (_script.Count > 0)
        {
            _last = _script.Dequeue();
        }

        foreach (var line in _last.Lines)
        {
            onOutput?.Invoke(line);
        }

        var outIndex = spec.Arguments.ToList().IndexOf("-o");
        if (_last.ExitCode == 0 && outIndex >= 0)
        {
            File.WriteAllBytes(spec.Arguments[outIndex + 1], [0x89, 0x50, 0x4E, 0x47]);
        }

        return Task.FromResult(new ProcessResult(_last.ExitCode, string.Join('\n', _last.Lines), string.Empty, TimeSpan.FromSeconds(1), false));
    }
}
