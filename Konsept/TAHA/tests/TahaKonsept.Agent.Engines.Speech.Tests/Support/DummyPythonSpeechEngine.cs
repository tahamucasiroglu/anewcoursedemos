using TahaKonsept.Agent.Engines.Speech.Base;

namespace TahaKonsept.Agent.Engines.Speech.Tests.Support;

/// <summary>
/// Testlere özel Python motoru: <c>adapters/dummy.py</c> (sinüs) kullanır, paket kurmaz. uv ile venv açıp işçi protokolünü
/// uçtan uca sınamak için.
/// </summary>
public sealed class DummyPythonSpeechEngine : PythonSpeechEngineBase
{
    /// <summary>Motoru kurar.</summary>
    /// <param name="services">Ortak bağımlılıklar.</param>
    public DummyPythonSpeechEngine(SpeechEngineServices services)
        : base(services)
    {
    }

    /// <inheritdoc />
    public override string Key => "dummy";

    /// <inheritdoc />
    public override string DisplayName => "Deneme";

    /// <inheritdoc />
    public override int? MinVramMb => null;

    /// <inheritdoc />
    public override string LanguageLabel => "Hepsi";

    /// <inheritdoc />
    public override IReadOnlyList<string> Languages { get; } = [];

    /// <inheritdoc />
    public override bool SupportsCloning => true;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, string> Voices { get; } = new Dictionary<string, string>();

    /// <inheritdoc />
    public override string AdapterName => "dummy";

    /// <inheritdoc />
    public override IReadOnlyList<string> Requirements { get; } = [];

    /// <inheritdoc />
    public override string? PythonVersion => null;
}
