using TahaKonsept.Agent.Engines.Speech.Base;

namespace TahaKonsept.Agent.Engines.Speech.Concrete.Python;

/// <summary>
/// Kokoro-82M (hexgrad, Apache-2.0; HF <c>hexgrad/Kokoro-82M</c>, pip <c>kokoro</c> + <c>misaki</c>). Türkçe YOK: İngilizce,
/// İspanyolca, Fransızca, Hintçe, İtalyanca, Japonca, Portekizce, Çince. CPU'da hızlı, 24 kHz. Python &lt; 3.13 ister.
/// </summary>
public sealed class KokoroSpeechEngine : PythonSpeechEngineBase
{
    private static readonly string[] Packages = ["torch==2.8.0", "kokoro==0.9.4", "misaki[en]==0.9.4", "soundfile"];

    /// <summary>Motoru kurar.</summary>
    /// <param name="services">Ortak bağımlılıklar.</param>
    public KokoroSpeechEngine(SpeechEngineServices services)
        : base(services)
    {
    }

    /// <inheritdoc />
    public override string Key => "kokoro";

    /// <inheritdoc />
    public override string DisplayName => "Kokoro";

    /// <inheritdoc />
    public override int? MinVramMb => null;

    /// <inheritdoc />
    public override string LanguageLabel => "Türkçe yok (İngilizce vb.)";

    /// <inheritdoc />
    public override IReadOnlyList<string> Languages { get; } = ["en", "es", "fr", "hi", "it", "ja", "pt", "zh"];

    /// <inheritdoc />
    public override bool SupportsCloning => false;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, string> Voices { get; } = new Dictionary<string, string>
    {
        ["af_heart"] = "Heart (ABD, kadın)",
        ["am_michael"] = "Michael (ABD, erkek)",
        ["bf_emma"] = "Emma (İngiltere, kadın)",
        ["bm_george"] = "George (İngiltere, erkek)",
        ["ef_dora"] = "Dora (İspanyolca, kadın)",
        ["ff_siwis"] = "Siwis (Fransızca, kadın)",
        ["if_sara"] = "Sara (İtalyanca, kadın)",
        ["pf_dora"] = "Dora (Portekizce, kadın)",
    };

    /// <inheritdoc />
    public override string AdapterName => "kokoro";

    /// <inheritdoc />
    public override string? ModelId => "hexgrad/Kokoro-82M";

    /// <inheritdoc />
    public override IReadOnlyList<string> Requirements => Packages;
}
