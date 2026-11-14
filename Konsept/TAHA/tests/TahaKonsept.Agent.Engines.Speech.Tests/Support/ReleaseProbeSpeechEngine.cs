using TahaKonsept.Agent.Engines.Speech.Base;
using TahaKonsept.Agent.Engines.Speech.Python.Abstract;

namespace TahaKonsept.Agent.Engines.Speech.Tests.Support;

/// <summary>
/// <see cref="PythonSpeechEngineBase.ReleaseAsync"/> testleri için motor: gerçek süreç açmaz, her işçi isteğinde yeni bir
/// <see cref="InMemoryTtsWorkerClient"/> (yükleme + iki ping yanıtı) verir ve kaç işçi açıldığını sayar (<see cref="WorkersCreated"/>).
/// <c>minVramMb</c> verilirse GPU kullanan (<see cref="SupportsGpu"/>), verilmezse GPU kullanamayan CPU motoru gibi davranır.
/// <c>loadResponse</c> işçinin <c>load</c> yanıtını belirler (ör. adapter'ın CPU'ya düştüğünü bildiren <c>device</c>/<c>device_note</c>).
/// </summary>
public sealed class ReleaseProbeSpeechEngine : PythonSpeechEngineBase
{
    private readonly int? _minVramMb;
    private readonly string _scriptPath;
    private readonly string _loadResponse;

    /// <summary>Motoru kurar.</summary>
    /// <param name="services">Ortak bağımlılıklar.</param>
    /// <param name="minVramMb">GPU belleği gereksinimi (<c>null</c> = CPU motoru).</param>
    /// <param name="scriptPath">Var olan bir dosya (işçi betiği yerine; varlık kontrolünü geçmek için).</param>
    /// <param name="loadResponse">İşçinin <c>load</c> yanıt satırı.</param>
    public ReleaseProbeSpeechEngine(SpeechEngineServices services, int? minVramMb, string scriptPath, string loadResponse = "{\"ok\": true}")
        : base(services)
    {
        _minVramMb = minVramMb;
        _scriptPath = scriptPath;
        _loadResponse = loadResponse;
    }

    /// <summary>Açılan işçi sayısı.</summary>
    public int WorkersCreated { get; private set; }

    /// <inheritdoc />
    public override string Key => "release-probe";

    /// <inheritdoc />
    public override string DisplayName => "Bırakma denemesi";

    /// <inheritdoc />
    public override int? MinVramMb => _minVramMb;

    /// <inheritdoc />
    public override string LanguageLabel => "Hepsi";

    /// <inheritdoc />
    public override IReadOnlyList<string> Languages { get; } = [];

    /// <inheritdoc />
    public override bool SupportsCloning => false;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, string> Voices { get; } = new Dictionary<string, string>();

    /// <inheritdoc />
    public override string AdapterName => "dummy";

    /// <inheritdoc />
    public override IReadOnlyList<string> Requirements { get; } = [];

    /// <inheritdoc />
    public override string WorkerScriptPath => _scriptPath;

    /// <inheritdoc />
    public override bool SupportsGpu => _minVramMb is not null;

    /// <inheritdoc />
    protected override ITtsWorkerClient CreateWorker()
    {
        WorkersCreated++;
        return new InMemoryTtsWorkerClient(_loadResponse + "\n{\"ok\": true, \"pong\": true}\n{\"ok\": true, \"pong\": true}\n");
    }
}
