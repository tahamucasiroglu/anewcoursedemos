namespace Konsept.Agent.Domain.Entities;

/// <summary>
/// Kurulum ekranındaki bir bileşen (spec 0009 Req 2, 11): sabit sürüm, resmi kaynak adresi ve ajana gömülü SHA-256. Ayna varsa
/// resmi kaynak erişilemezken Pi'deki kopyadan (<see cref="FileName"/> adıyla) indirilir (Req 12).
/// </summary>
/// <param name="Id">Teknik ad (klasör adı; ör. <c>node</c>).</param>
/// <param name="Name">Görünen ad (ör. "Node.js").</param>
/// <param name="Purpose">Ne işe yaradığı (Türkçe, kısa).</param>
/// <param name="License">Lisans adı.</param>
/// <param name="Version">Sabit sürüm.</param>
/// <param name="Url">Resmi kaynak adresi.</param>
/// <param name="FileName">İndirilen arşivin adı (aynada da bu ad).</param>
/// <param name="Sha256">Beklenen SHA-256 (küçük harf onaltılık).</param>
/// <param name="DownloadBytes">İndirme boyutu.</param>
/// <param name="InstalledBytes">Açılmış boyut.</param>
/// <param name="ArchiveRoot">Arşivin içindeki kök klasör (kurulumda atılır); yoksa <c>null</c>.</param>
/// <param name="Executable">Bileşen klasörüne göre yürütülebilir dosya (bütünlük denetimi ve Hazır denetimi).</param>
/// <param name="CheckArguments">Hazır denetimi argümanları; <c>null</c> ise yalnızca hash.</param>
/// <param name="CheckExpect">Hazır denetimi çıktısında beklenen metin (sabit sürüm).</param>
/// <param name="Required">Zorunlu çekirdek bileşen mi.</param>
/// <param name="Mirrored">Pi'de aynası var mı.</param>
/// <param name="Installer">Kurulum biçimi: <c>null</c> arşiv (indir → SHA-256 → aç); <c>npm</c> ajana gömülü kilit dosyasıyla kurulu Node'un npm'i
/// (<c>npm ci --ignore-scripts</c>; her paketin bütünlüğü kilit dosyasındaki <c>integrity</c> ile, kilit dosyası <see cref="Sha256"/> ile doğrulanır;
/// spec 0018 / B-118); <c>python</c> ajana gömülü hash'li kilit dosyasıyla taşınabilir Python'a paket ortamı (<c>uv pip install --require-hashes
/// --target</c>; spec 0028); <c>files</c> çok dosyalı model ağırlıkları (<see cref="Files"/>; spec 0028).</param>
/// <param name="Requires">Önce kurulu olması gereken bileşenler (spec 0028 Req 2); yoksa <c>null</c>.</param>
/// <param name="Files"><c>files</c> biçiminde indirilecek dosyalar; <see cref="Sha256"/> bunların listesinin (<see cref="Rules.ComponentRules.FilesManifest"/>) hash'idir.</param>
/// <param name="Gpu">Ekran kartı gereksinimi; yoksa <c>null</c> (spec 0028 Req 6).</param>
/// <param name="Run">Yerel model çalıştırma şablonu; model değilse <c>null</c> (spec 0028 Req 7, 8).</param>
/// <param name="Variants">Donanıma göre seçilen biçimler; yoksa <c>null</c> (spec 0028 Req 5).</param>
/// <param name="Variant">Seçilmiş varyant (katalog çözümünde atanır).</param>
public sealed record SetupComponent(
    string Id,
    string Name,
    string Purpose,
    string License,
    string Version,
    Uri Url,
    string FileName,
    string Sha256,
    long DownloadBytes,
    long InstalledBytes,
    string? ArchiveRoot,
    string Executable,
    string? CheckArguments,
    string? CheckExpect,
    bool Required,
    bool Mirrored,
    string? Installer = null,
    IReadOnlyList<string>? Requires = null,
    IReadOnlyList<ComponentFile>? Files = null,
    GpuRequirement? Gpu = null,
    ModelRun? Run = null,
    IReadOnlyList<ComponentVariant>? Variants = null,
    string? Variant = null)
{
    /// <summary>npm kurulum biçimi.</summary>
    public const string Npm = "npm";

    /// <summary>Python paket ortamı kurulum biçimi (spec 0028).</summary>
    public const string Python = "python";

    /// <summary>Çok dosyalı kurulum biçimi (spec 0028).</summary>
    public const string FilesInstaller = "files";

    /// <summary>Çok dosyalı bileşende kurulumda yazılan dosya listesi (bütünlük kaydı; <see cref="Executable"/> budur).</summary>
    public const string FilesManifestName = "files.sha256";

    /// <summary>Taşınabilir Python bileşeninin kimliği (Python ortamları bunu kullanır).</summary>
    public const string PythonRuntimeId = "python";

    /// <summary>Paket kurucu (uv) bileşeninin kimliği.</summary>
    public const string UvId = "uv";
}
