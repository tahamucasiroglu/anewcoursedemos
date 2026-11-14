using TahaKonsept.Agent.Engines.Speech.Base;

namespace TahaKonsept.Agent.Engines.Speech.Concrete.Python;

/// <summary>
/// Chatterbox Multilingual (Resemble AI, MIT; HF <c>ResembleAI/chatterbox</c>; pip <c>chatterbox-tts</c>). Referans sesle sıfır-atış
/// klonlama, 23 dil (Türkçe dahil), ~4 GB VRAM. Çıktıda Perth filigranı vardır. torch 2.6 (paketin sabitlediği) + CUDA 12.4.
/// Hız kontrolü yoktur (gerekirse birleştirmede atempo).
/// </summary>
public sealed class ChatterboxSpeechEngine : PythonSpeechEngineBase
{
    private static readonly string[] Packages = ["torch==2.6.0", "torchaudio==2.6.0", "chatterbox-tts==0.1.7"];

    private static readonly string[] Codes =
        ["ar", "da", "de", "el", "en", "es", "fi", "fr", "he", "hi", "it", "ja", "ko", "ms", "nl", "no", "pl", "pt", "ru", "sv", "sw", "tr", "zh"];

    /// <summary>Motoru kurar.</summary>
    /// <param name="services">Ortak bağımlılıklar.</param>
    public ChatterboxSpeechEngine(SpeechEngineServices services)
        : base(services)
    {
    }

    /// <inheritdoc />
    public override string Key => "chatterbox-multilingual";

    /// <inheritdoc />
    public override string DisplayName => "Chatterbox Multilingual";

    /// <inheritdoc />
    public override int? MinVramMb => 4096;

    /// <inheritdoc />
    public override string LanguageLabel => "Çok dilli (23 dil), klonlama";

    /// <inheritdoc />
    public override IReadOnlyList<string> Languages => Codes;

    /// <inheritdoc />
    public override bool SupportsCloning => true;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, string> Voices { get; } = new Dictionary<string, string> { ["default"] = "Varsayılan ses" };

    /// <inheritdoc />
    public override string AdapterName => "chatterbox";

    /// <inheritdoc />
    public override IReadOnlyList<string> Requirements => Packages;

    /// <inheritdoc />
    public override string? TorchIndexUrl => TorchCu124Index;
}
