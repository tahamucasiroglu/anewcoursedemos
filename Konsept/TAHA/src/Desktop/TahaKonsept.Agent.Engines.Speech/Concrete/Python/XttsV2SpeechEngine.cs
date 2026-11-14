using TahaKonsept.Agent.Engines.Speech.Base;

namespace TahaKonsept.Agent.Engines.Speech.Concrete.Python;

/// <summary>
/// XTTS-v2 (Coqui; bakımı süren idiap çatalı, pip <c>coqui-tts</c>; model <c>tts_models/multilingual/multi-dataset/xtts_v2</c>,
/// HF <c>coqui/XTTS-v2</c>). Lisans CPML (ticari değil). Referans sesle klonlama, 17 dil (Türkçe dahil, kalite zayıf), 2–4 GB VRAM.
/// Referans yoksa yerleşik konuşmacılardan biri (<see cref="Voices"/>) kullanılır. <c>six</c> ayrıca kurulur: coqui-tts 0.28.0
/// <c>TTS/tts/utils/text/punctuation.py</c> içinde <c>import six</c> yapar ama bağımlılık olarak bildirmez (yoksa yükleme
/// "No module named 'six'" ile düşer).
/// </summary>
public sealed class XttsV2SpeechEngine : PythonSpeechEngineBase
{
    private static readonly string[] Packages = ["torch==2.8.0", "torchaudio==2.8.0", "coqui-tts==0.28.0", "six"];

    private static readonly string[] Codes =
        ["en", "es", "fr", "de", "it", "pt", "pl", "tr", "ru", "nl", "cs", "ar", "zh", "ja", "hu", "ko", "hi"];

    /// <summary>Motoru kurar.</summary>
    /// <param name="services">Ortak bağımlılıklar.</param>
    public XttsV2SpeechEngine(SpeechEngineServices services)
        : base(services)
    {
    }

    /// <inheritdoc />
    public override string Key => "xtts-v2";

    /// <inheritdoc />
    public override string DisplayName => "XTTS-v2";

    /// <inheritdoc />
    public override int? MinVramMb => 3072;

    /// <inheritdoc />
    public override string LanguageLabel => "Çok dilli (17 dil), klonlama";

    /// <inheritdoc />
    public override IReadOnlyList<string> Languages => Codes;

    /// <inheritdoc />
    public override bool SupportsCloning => true;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, string> Voices { get; } = new Dictionary<string, string>
    {
        ["Ana Florence"] = "Ana Florence (kadın)",
        ["Claribel Dervla"] = "Claribel Dervla (kadın)",
        ["Damien Black"] = "Damien Black (erkek)",
        ["Viktor Eka"] = "Viktor Eka (erkek)",
    };

    /// <inheritdoc />
    public override string AdapterName => "xtts";

    /// <inheritdoc />
    public override IReadOnlyList<string> Requirements => Packages;

    /// <inheritdoc />
    public override string? TorchIndexUrl => TorchCu128Index;
}
