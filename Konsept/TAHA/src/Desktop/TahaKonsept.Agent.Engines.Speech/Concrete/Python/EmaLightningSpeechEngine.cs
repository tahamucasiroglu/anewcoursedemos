using TahaKonsept.Agent.Engines.Speech.Base;

namespace TahaKonsept.Agent.Engines.Speech.Concrete.Python;

/// <summary>
/// EMA Lightning (Apache-2.0, ~34 MB, HF <c>canberkkkkkk/ema-lightning</c>, pip <c>ema-lightning</c>). Sadece Türkçe, tek ses,
/// CPU'da gerçek zamandan hızlı, 48 kHz. Model kartı sentetik ses bildirimi ister.
/// </summary>
public sealed class EmaLightningSpeechEngine : PythonSpeechEngineBase
{
    private static readonly string[] Packages = ["torch==2.8.0", "ema-lightning==1.1.2"];

    /// <summary>Motoru kurar.</summary>
    /// <param name="services">Ortak bağımlılıklar.</param>
    public EmaLightningSpeechEngine(SpeechEngineServices services)
        : base(services)
    {
    }

    /// <inheritdoc />
    public override string Key => "ema-lightning";

    /// <inheritdoc />
    public override string DisplayName => "EMA Lightning";

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
    public override string AdapterName => "ema_lightning";

    /// <inheritdoc />
    public override IReadOnlyList<string> Requirements => Packages;
}
