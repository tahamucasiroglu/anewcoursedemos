using TahaKonsept.Agent.Engines.Speech.Base;

namespace TahaKonsept.Agent.Engines.Speech.Concrete.Python;

/// <summary>
/// Trendyol-TTS (deneysel; HF <c>Trendyol/Trendyol-TTS</c>): VoxCPM2 + birleştirilmiş Türkçe LoRA, pip <c>voxcpm</c>.
/// Sadece Türkçe, GPU (~8 GB), lisans belirsiz. cfg 2.0, 16 adım.
/// </summary>
public sealed class TrendyolSpeechEngine : PythonSpeechEngineBase
{
    private static readonly string[] Packages = ["torch==2.8.0", "torchaudio==2.8.0", "voxcpm==2.0.3", "soundfile"];

    /// <summary>Motoru kurar.</summary>
    /// <param name="services">Ortak bağımlılıklar.</param>
    public TrendyolSpeechEngine(SpeechEngineServices services)
        : base(services)
    {
    }

    /// <inheritdoc />
    public override string Key => "trendyol-tts";

    /// <inheritdoc />
    public override string DisplayName => "Trendyol-TTS";

    /// <inheritdoc />
    public override int? MinVramMb => 8192;

    /// <inheritdoc />
    public override string LanguageLabel => "Sadece Türkçe, deneysel";

    /// <inheritdoc />
    public override IReadOnlyList<string> Languages { get; } = ["tr"];

    /// <inheritdoc />
    public override bool SupportsCloning => false;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, string> Voices { get; } = new Dictionary<string, string> { ["default"] = "Türkçe ses" };

    /// <inheritdoc />
    public override string AdapterName => "trendyol";

    /// <inheritdoc />
    public override string? ModelId => "Trendyol/Trendyol-TTS";

    /// <inheritdoc />
    public override IReadOnlyList<string> Requirements => Packages;

    /// <inheritdoc />
    public override string? TorchIndexUrl => TorchCu128Index;
}
