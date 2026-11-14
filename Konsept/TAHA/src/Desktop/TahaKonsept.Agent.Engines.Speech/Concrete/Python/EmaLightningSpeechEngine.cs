using TahaKonsept.Agent.Engines.Speech.Base;

namespace TahaKonsept.Agent.Engines.Speech.Concrete.Python;

/// <summary>
/// EMA Lightning (Apache-2.0, ~34 MB, HF <c>canberkkkkkk/ema-lightning</c>, pip <c>ema-lightning</c>). Sadece Türkçe, tek ses,
/// 48 kHz. Model kartı sentetik ses bildirimi ister. "Önce GPU": GPU varsa CUDA torch tekerleği, model <c>cuda</c>'da ve CUDA grafik
/// hızlandırması (<c>lightning()</c>) denenir; GPU yoksa uyarıyla CPU (CPU'da da gerçek zamandan hızlı).
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

    /// <summary>GPU seçildiyse torch 2.8.0 CUDA 12.8 tekerleği (GPU yoksa CPU tekerleği; parmak izi değişince yeniden kurulur).</summary>
    public override string? TorchIndexUrl => CudaTorchIndex(TorchCu128Index);
}
