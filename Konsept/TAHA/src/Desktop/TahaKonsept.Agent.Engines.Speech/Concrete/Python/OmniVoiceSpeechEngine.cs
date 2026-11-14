using TahaKonsept.Agent.Engines.Speech.Base;

namespace TahaKonsept.Agent.Engines.Speech.Concrete.Python;

/// <summary>
/// OmniVoice (k2-fsa, ~0.6B; HF <c>k2-fsa/OmniVoice</c>, pip <c>omnivoice</c>). 600+ dil, referans sesle klonlama;
/// referans yoksa ses tarifiyle (instruct) üretir (<see cref="Voices"/>). Ağırlık lisansı netleşmedi.
/// </summary>
public sealed class OmniVoiceSpeechEngine : PythonSpeechEngineBase
{
    private static readonly string[] Packages = ["torch==2.8.0", "torchaudio==2.8.0", "omnivoice==0.2.1"];

    /// <summary>Motoru kurar.</summary>
    /// <param name="services">Ortak bağımlılıklar.</param>
    public OmniVoiceSpeechEngine(SpeechEngineServices services)
        : base(services)
    {
    }

    /// <inheritdoc />
    public override string Key => "omnivoice";

    /// <inheritdoc />
    public override string DisplayName => "OmniVoice";

    /// <inheritdoc />
    public override int? MinVramMb => 4096;

    /// <inheritdoc />
    public override string LanguageLabel => "Çok dilli (600+ dil), klonlama";

    /// <inheritdoc />
    public override IReadOnlyList<string> Languages { get; } = [];

    /// <inheritdoc />
    public override bool SupportsCloning => true;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, string> Voices { get; } = new Dictionary<string, string>
    {
        ["male, middle-aged, moderate pitch"] = "Erkek (orta yaş)",
        ["male, young adult, low pitch"] = "Erkek (genç, kalın)",
        ["female, middle-aged, moderate pitch"] = "Kadın (orta yaş)",
        ["female, young adult, high pitch"] = "Kadın (genç, ince)",
    };

    /// <inheritdoc />
    public override string AdapterName => "omnivoice";

    /// <inheritdoc />
    public override string? ModelId => "k2-fsa/OmniVoice";

    /// <inheritdoc />
    public override IReadOnlyList<string> Requirements => Packages;

    /// <inheritdoc />
    public override string? TorchIndexUrl => TorchCu128Index;
}
