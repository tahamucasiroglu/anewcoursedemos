using TahaKonsept.Agent.Engines.Speech.Base;

namespace TahaKonsept.Agent.Engines.Speech.Concrete.Python;

/// <summary>
/// Meta MMS-TTS Türkçe (VITS, HF <c>facebook/mms-tts-tur</c>, transformers <c>VitsModel</c>). Lisans cc-by-nc-4.0.
/// Sadece Türkçe, 16 kHz. "Önce GPU": GPU varsa CUDA torch tekerleği + model <c>cuda</c>'da; yoksa uyarıyla CPU.
/// </summary>
public sealed class MmsTurkishSpeechEngine : PythonSpeechEngineBase
{
    private static readonly string[] Packages = ["torch==2.8.0", "transformers==4.56.2", "sentencepiece"];

    /// <summary>Motoru kurar.</summary>
    /// <param name="services">Ortak bağımlılıklar.</param>
    public MmsTurkishSpeechEngine(SpeechEngineServices services)
        : base(services)
    {
    }

    /// <inheritdoc />
    public override string Key => "mms-tts-tr";

    /// <inheritdoc />
    public override string DisplayName => "MMS-TTS Türkçe";

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
    public override string AdapterName => "mms";

    /// <inheritdoc />
    public override string? ModelId => "facebook/mms-tts-tur";

    /// <inheritdoc />
    public override IReadOnlyList<string> Requirements => Packages;

    /// <summary>GPU seçildiyse torch 2.8.0 CUDA 12.8 tekerleği (GPU yoksa CPU tekerleği; parmak izi değişince yeniden kurulur).</summary>
    public override string? TorchIndexUrl => CudaTorchIndex(TorchCu128Index);
}
