using TahaKonsept.Agent.Engines.Speech.Base;

namespace TahaKonsept.Agent.Engines.Speech.Concrete.Python;

/// <summary>
/// Ahenk (ağırlıklar CC BY 4.0, atıf gerekir): VoxCPM2 2B + Türkçe LoRA (HF <c>ozgurguler/ahenk-tts-tr</c>), metin <c>trnorm</c> ile
/// okunuşa çevrilir. Sadece Türkçe, GPU (~8 GB). Ses kimliği = duygu (<see cref="Voices"/>). Git ile paket kurar (sistemde git gerekir).
/// </summary>
public sealed class AhenkSpeechEngine : PythonSpeechEngineBase
{
    private static readonly string[] Packages =
    [
        "torch==2.8.0",
        "torchaudio==2.8.0",
        "voxcpm @ git+https://github.com/OpenBMB/VoxCPM@f0c787f0937dc1c9a8f4f64d9a332d9c5da2e629",
        "git+https://github.com/ozguradmin/ahenk-turkish-tts",
        "soundfile",
        "scipy",
        "huggingface_hub",
    ];

    /// <summary>Motoru kurar.</summary>
    /// <param name="services">Ortak bağımlılıklar.</param>
    public AhenkSpeechEngine(SpeechEngineServices services)
        : base(services)
    {
    }

    /// <inheritdoc />
    public override string Key => "ahenk";

    /// <inheritdoc />
    public override string DisplayName => "Ahenk";

    /// <inheritdoc />
    public override int? MinVramMb => 8192;

    /// <inheritdoc />
    public override string LanguageLabel => "Sadece Türkçe";

    /// <inheritdoc />
    public override IReadOnlyList<string> Languages { get; } = ["tr"];

    /// <inheritdoc />
    public override bool SupportsCloning => false;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, string> Voices { get; } = new Dictionary<string, string>
    {
        ["neutral"] = "Düz",
        ["cheerful"] = "Neşeli",
        ["angry"] = "Kızgın",
        ["sad"] = "Hüzünlü",
    };

    /// <inheritdoc />
    public override string AdapterName => "ahenk";

    /// <inheritdoc />
    public override string? ModelId => "ahenk";

    /// <inheritdoc />
    public override IReadOnlyList<string> Requirements => Packages;

    /// <inheritdoc />
    public override string? TorchIndexUrl => TorchCu128Index;
}
