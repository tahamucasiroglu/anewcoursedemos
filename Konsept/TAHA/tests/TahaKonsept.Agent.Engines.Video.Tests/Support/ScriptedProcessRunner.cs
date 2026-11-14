using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;

namespace TahaKonsept.Agent.Engines.Video.Tests.Support;

/// <summary>
/// Süreç başlatmayan sahte çalıştırıcı: her çağrıyı <see cref="Calls"/> listesine yazar ve sonucu verilen betikten üretir
/// (ör. "argümanlarda h264_nvenc varsa başarısız"). GPU'suz kodlayıcı seçimi/yedek testleri için.
/// </summary>
public sealed class ScriptedProcessRunner : IProcessRunner
{
    private readonly Func<ProcessSpec, ProcessResult> _script;

    /// <summary>Betikle kurar.</summary>
    /// <param name="script">Çağrı → sonuç.</param>
    public ScriptedProcessRunner(Func<ProcessSpec, ProcessResult> script)
    {
        _script = script;
    }

    /// <summary>Yapılan çağrılar (sıralı).</summary>
    public List<ProcessSpec> Calls { get; } = [];

    /// <summary>Başarılı sonuç.</summary>
    /// <param name="stdout">Standart çıktı.</param>
    /// <returns>Sonuç.</returns>
    public static ProcessResult Ok(string stdout = "") => new(0, stdout, string.Empty, TimeSpan.Zero, false);

    /// <summary>Başarısız sonuç.</summary>
    /// <param name="stderr">Hata çıktısı.</param>
    /// <returns>Sonuç.</returns>
    public static ProcessResult Fail(string stderr) => new(1, string.Empty, stderr, TimeSpan.Zero, false);

    /// <inheritdoc />
    public Task<ProcessResult> RunAsync(ProcessSpec spec, Action<string>? onOutput, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(spec);
        lock (Calls)
        {
            Calls.Add(spec);
        }

        return Task.FromResult(_script(spec));
    }
}
