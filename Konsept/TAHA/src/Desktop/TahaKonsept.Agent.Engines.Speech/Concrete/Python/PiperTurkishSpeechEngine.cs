using TahaKonsept.Agent.Engines.Speech.Base;

namespace TahaKonsept.Agent.Engines.Speech.Concrete.Python;

/// <summary>
/// Piper Türkçe (ONNX VITS; pip <c>piper-tts</c>, sesler HF <c>rhasspy/piper-voices</c> <c>tr/tr_TR/…</c>). Ses lisansı CC BY-NC-SA.
/// Sadece Türkçe, CPU, çok hızlı ama robotik. torch gerekmez.
/// </summary>
public sealed class PiperTurkishSpeechEngine : PythonSpeechEngineBase
{
    private static readonly string[] Packages = ["piper-tts==1.8.0", "huggingface_hub"];

    /// <summary>Motoru kurar.</summary>
    /// <param name="services">Ortak bağımlılıklar.</param>
    public PiperTurkishSpeechEngine(SpeechEngineServices services)
        : base(services)
    {
    }

    /// <inheritdoc />
    public override string Key => "piper-tr";

    /// <inheritdoc />
    public override string DisplayName => "Piper Türkçe";

    /// <inheritdoc />
    public override int? MinVramMb => null;

    /// <inheritdoc />
    public override string LanguageLabel => "Sadece Türkçe";

    /// <inheritdoc />
    public override IReadOnlyList<string> Languages { get; } = ["tr"];

    /// <inheritdoc />
    public override bool SupportsCloning => false;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, string> Voices { get; } = new Dictionary<string, string>
    {
        ["tr_TR-dfki-medium"] = "DFKI (orta kalite)",
        ["tr_TR-fahrettin-medium"] = "Fahrettin (orta kalite)",
        ["tr_TR-fettah-medium"] = "Fettah (orta kalite)",
    };

    /// <inheritdoc />
    public override string AdapterName => "piper";

    /// <inheritdoc />
    public override string? ModelId => "tr_TR-dfki-medium";

    /// <inheritdoc />
    public override IReadOnlyList<string> Requirements => Packages;
}
