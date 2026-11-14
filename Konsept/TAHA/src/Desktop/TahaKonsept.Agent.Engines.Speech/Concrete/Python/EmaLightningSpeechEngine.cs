using TahaKonsept.Agent.Engines.Speech.Base;

namespace TahaKonsept.Agent.Engines.Speech.Concrete.Python;

/// <summary>
/// EMA Lightning (Apache-2.0, ~34 MB, HF <c>canberkkkkkk/ema-lightning</c>, pip <c>ema-lightning</c>). Sadece Türkçe, tek ses,
/// 48 kHz. Model kartı sentetik ses bildirimi ister. "Önce GPU": GPU varsa CUDA torch tekerleği ve model <c>cuda</c>'da; GPU yoksa
/// uyarıyla CPU (CPU'da da gerçek zamandan hızlı). CUDA grafik hızlandırması (<c>lightning()</c>) kapalıdır (<see cref="LoadOptions"/>):
/// RTX 3060'ta düz CUDA 4 sahneyi 1,0 sn'de / 132 MB VRAM ile seslendirirken <c>lightning()</c> her yüklemede 28 sn (ilk kez ~170 sn
/// toplu boyut ölçümü) ve 2,9–5,7 GB VRAM harcayıp yalnız 0,8 sn kazandırıyor; GPU işçisi her işten sonra kapandığı için hiç geri ödemez.
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

    /// <summary>Adapter seçenekleri: <c>lightning=false</c> (düz CUDA yolu; gerekçe sınıf özetinde).</summary>
    public override IReadOnlyDictionary<string, object?> LoadOptions { get; } = new Dictionary<string, object?> { ["lightning"] = false };

    /// <summary>GPU seçildiyse torch 2.8.0 CUDA 12.8 tekerleği (GPU yoksa CPU tekerleği; parmak izi değişince yeniden kurulur).</summary>
    public override string? TorchIndexUrl => CudaTorchIndex(TorchCu128Index);
}
