using TahaKonsept.Agent.Engines.Speech.Base;

namespace TahaKonsept.Agent.Engines.Speech.Concrete.Python;

/// <summary>
/// Piper Türkçe (ONNX VITS; pip <c>piper-tts</c>, sesler HF <c>rhasspy/piper-voices</c> <c>tr/tr_TR/…</c>). Ses lisansı CC BY-NC-SA.
/// Sadece Türkçe, çok hızlı ama robotik. torch gerekmez. "Önce GPU" kuralının belgeli istisnası: CPU'da çalışır (<see cref="SupportsGpu"/>).
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

    /// <summary>
    /// <c>false</c>: piper-tts CPU <c>onnxruntime</c> tekerleğine bağlı; GPU tekerlekleri (<c>onnxruntime-gpu</c> / <c>-directml</c>) aynı
    /// <c>onnxruntime</c> modülüne kurulduğu için tek venv'de temiz bir arada durmaz, CUDA yolu ayrıca ~1 GB cuDNN/cuBLAS tekerleği ister.
    /// ~60 MB VITS modeli CPU'da zaten gerçek zamandan kat kat hızlı; bu yüzden tasarım gereği CPU (loglanır, uyarı değil).
    /// </summary>
    public override bool SupportsGpu => false;
}
