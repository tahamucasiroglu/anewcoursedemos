using TahaKonsept.Agent.Engines.Speech.Base;

namespace TahaKonsept.Agent.Engines.Speech.Concrete.Python;

/// <summary>
/// F5-TTS Türkçe ince ayarı (HF <c>marduk-ra/F5-TTS-Turkish</c>, <c>f5_tts_turkish_1000000.safetensors</c> + <c>vocab.txt</c>;
/// pip <c>f5-tts</c>). Lisans cc-by-nc-4.0. Akış eşleme, referans ses + metniyle klonlar; ~3 GB VRAM, NFE 64.
/// </summary>
public sealed class F5TtsTurkishSpeechEngine : PythonSpeechEngineBase
{
    private static readonly string[] Packages = ["torch==2.8.0", "torchaudio==2.8.0", "f5-tts==1.1.22"];

    /// <summary>Motoru kurar.</summary>
    /// <param name="services">Ortak bağımlılıklar.</param>
    public F5TtsTurkishSpeechEngine(SpeechEngineServices services)
        : base(services)
    {
    }

    /// <inheritdoc />
    public override string Key => "f5-tts-tr";

    /// <inheritdoc />
    public override string DisplayName => "F5-TTS Türkçe";

    /// <inheritdoc />
    public override int? MinVramMb => 3072;

    /// <inheritdoc />
    public override string LanguageLabel => "Sadece Türkçe, klonlama";

    /// <inheritdoc />
    public override IReadOnlyList<string> Languages { get; } = ["tr"];

    /// <inheritdoc />
    public override bool SupportsCloning => true;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, string> Voices { get; } = new Dictionary<string, string>();

    /// <inheritdoc />
    public override string AdapterName => "f5_tts";

    /// <inheritdoc />
    public override string? ModelId => "marduk-ra/F5-TTS-Turkish";

    /// <inheritdoc />
    public override IReadOnlyList<string> Requirements => Packages;

    /// <inheritdoc />
    public override string? TorchIndexUrl => TorchCu128Index;
}
