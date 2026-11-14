using TahaKonsept.Agent.Engines.Speech.Base;

namespace TahaKonsept.Agent.Engines.Speech.Concrete.Python;

/// <summary>
/// Ahenk Cep (ağırlıklar CC BY 4.0): 8.6M parametreli öğrenci model (HF <c>ozgurguler/ahenk-cep-tr</c>, EMA Lightning 1.1.2 tabanlı).
/// Sadece Türkçe, CPU. Git ile paket kurar (sistemde git gerekir).
/// </summary>
public sealed class AhenkCepSpeechEngine : PythonSpeechEngineBase
{
    private static readonly string[] Packages =
    [
        "torch==2.8.0",
        "ema-lightning==1.1.2",
        "git+https://github.com/ozguradmin/ahenk-turkish-tts",
        "soundfile",
        "huggingface_hub",
    ];

    /// <summary>Motoru kurar.</summary>
    /// <param name="services">Ortak bağımlılıklar.</param>
    public AhenkCepSpeechEngine(SpeechEngineServices services)
        : base(services)
    {
    }

    /// <inheritdoc />
    public override string Key => "ahenk-cep";

    /// <inheritdoc />
    public override string DisplayName => "Ahenk Cep";

    /// <inheritdoc />
    public override int? MinVramMb => null;

    /// <inheritdoc />
    public override string LanguageLabel => "Sadece Türkçe";

    /// <inheritdoc />
    public override IReadOnlyList<string> Languages { get; } = ["tr"];

    /// <inheritdoc />
    public override bool SupportsCloning => false;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, string> Voices { get; } = new Dictionary<string, string> { ["default"] = "Türkçe ses" };

    /// <inheritdoc />
    public override string AdapterName => "ahenk";

    /// <inheritdoc />
    public override string? ModelId => "ahenk-cep";

    /// <inheritdoc />
    public override IReadOnlyList<string> Requirements => Packages;
}
