using TahaKonsept.Agent.Engines.Speech.Base;

namespace TahaKonsept.Agent.Engines.Speech.Concrete.Python;

/// <summary>
/// VoxCPM2 (OpenBMB, Apache-2.0, 2B; HF <c>openbmb/VoxCPM2</c>, pip <c>voxcpm</c>). Çok dilli (Türkçe WER 2.50), referans sesle
/// klonlama (<c>reference_wav_path</c>). En az 8 GB VRAM (6 GB'a sığmaz).
/// </summary>
public sealed class VoxCpm2SpeechEngine : PythonSpeechEngineBase
{
    private static readonly string[] Packages = ["torch==2.8.0", "torchaudio==2.8.0", "voxcpm==2.0.3", "soundfile"];

    /// <summary>Motoru kurar.</summary>
    /// <param name="services">Ortak bağımlılıklar.</param>
    public VoxCpm2SpeechEngine(SpeechEngineServices services)
        : base(services)
    {
    }

    /// <inheritdoc />
    public override string Key => "voxcpm2";

    /// <inheritdoc />
    public override string DisplayName => "VoxCPM2";

    /// <inheritdoc />
    public override int? MinVramMb => 8192;

    /// <inheritdoc />
    public override string LanguageLabel => "Çok dilli (Türkçe dahil), klonlama";

    /// <inheritdoc />
    public override IReadOnlyList<string> Languages { get; } = [];

    /// <inheritdoc />
    public override bool SupportsCloning => true;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, string> Voices { get; } = new Dictionary<string, string> { ["default"] = "Varsayılan ses" };

    /// <inheritdoc />
    public override string AdapterName => "voxcpm";

    /// <inheritdoc />
    public override string? ModelId => "openbmb/VoxCPM2";

    /// <inheritdoc />
    public override IReadOnlyList<string> Requirements => Packages;

    /// <inheritdoc />
    public override string? TorchIndexUrl => TorchCu128Index;
}
