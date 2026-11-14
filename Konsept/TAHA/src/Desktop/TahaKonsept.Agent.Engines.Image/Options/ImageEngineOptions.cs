namespace TahaKonsept.Agent.Engines.Image.Options;

/// <summary>
/// Yerel görsel motorlarının ayarları. Klasörler <c>IAgentPaths</c> altına göreli verilir:
/// modeller <c>ModelsDir/{ModelsSubdirectory}</c>, sd.cpp <c>ToolsDir/{SdCppToolSubdirectory}</c>.
/// </summary>
public sealed class ImageEngineOptions
{
    /// <summary>Görsel motorlarının kullandığı adlandırılmış <c>HttpClient</c>.</summary>
    public const string HttpClientName = "TahaKonsept.ImageEngines";

    /// <summary>Model dosyalarının <c>IAgentPaths.ModelsDir</c> altındaki klasörü.</summary>
    public string ModelsSubdirectory { get; set; } = "image";

    /// <summary>sd.cpp ikililerinin <c>IAgentPaths.ToolsDir</c> altındaki klasörü.</summary>
    public string SdCppToolSubdirectory { get; set; } = "sd-cpp";

    /// <summary>sd.cpp hızlandırıcı tercihi.</summary>
    public SdCppBackendPreference SdCppBackend { get; set; } = SdCppBackendPreference.Auto;

    /// <summary>sd.cpp son sürüm bilgisi (GitHub releases API).</summary>
    public string SdCppReleaseApiUrl { get; set; } = "https://api.github.com/repos/leejet/stable-diffusion.cpp/releases/latest";

    /// <summary>Hugging Face temel adresi (dosya: <c>{base}/{repo}/resolve/main/{path}</c>).</summary>
    public string HuggingFaceBaseUrl { get; set; } = "https://huggingface.co";

    /// <summary>Kapılı (gated) depolar için isteğe bağlı Hugging Face erişim anahtarı.</summary>
    public string? HuggingFaceToken { get; set; }

    /// <summary>Düşük VRAM bayrakları (<c>--offload-to-cpu</c>, difüzyonda flash attention, <c>--vae-tiling</c>); 6 GB için açık.</summary>
    public bool LowVram { get; set; } = true;

    /// <summary>Üretim sırasında eksik model dosyaları otomatik indirilsin mi.</summary>
    public bool AutoDownloadModels { get; set; } = true;

    /// <summary>EnsureReadyAsync'in hazırlayacağı varsayılan model.</summary>
    public string DefaultModelKey { get; set; } = "z-image-turbo";

    /// <summary>Tek görsel üretimi için zaman aşımı.</summary>
    public TimeSpan GenerationTimeout { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Kullanıcının kendi çalıştırdığı ComfyUI sunucusu.</summary>
    public string ComfyUiBaseUrl { get; set; } = "http://127.0.0.1:8188";

    /// <summary>ComfyUI geçmişi yoklama aralığı.</summary>
    public TimeSpan ComfyUiPollInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>İndirme denemesi sayısı (kesintide kaldığı yerden devam eder).</summary>
    public int DownloadRetryCount { get; set; } = 3;
}
