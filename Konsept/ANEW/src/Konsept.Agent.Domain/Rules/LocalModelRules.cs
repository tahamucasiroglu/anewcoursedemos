using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Konsept.Agent.Domain.Rules;

/// <summary>
/// Yerel model aşamalarının kuralları (spec 0028 Req 7, 8, 10): görsel boyutu biçime göre, sahne tohumu, devam işaretleri (aynı girdiyle yeniden
/// üretilmez) ve süreç çıktısında ekran kartı/bellek hatası tanıma (CPU'ya düşülmez, "Ekran kartı kullanılamadı").
/// </summary>
public static class LocalModelRules
{
    /// <summary>Sahne görselinin dosya adı (render kodu bunu kullanır).</summary>
    public const string ImageFile = "image.png";

    /// <summary>Görsel işaret dosyası.</summary>
    public const string ImageMarkerFile = "image.marker";

    private static readonly string[] GpuFailureSignatures =
    [
        "ErrorOutOfDeviceMemory", "ErrorDeviceLost", "VK_ERROR", "vk::", "out of memory", "failed to allocate", "No Vulkan devices",
        "CUDA error", "cudaMalloc", "CUBLAS_STATUS",
    ];

    /// <summary>Görsel boyutu: yatay 1024×576, dikey 576×1024 (16'nın katı; video biçimiyle aynı oran).</summary>
    /// <param name="format"><c>portrait</c> ya da <c>landscape</c>.</param>
    /// <returns>Genişlik, yükseklik.</returns>
    public static (int Width, int Height) ImageSize(string format) =>
        string.Equals(format, "portrait", StringComparison.Ordinal) ? (576, 1024) : (1024, 576);

    /// <summary>Sahnenin sabit tohumu (aynı sahne aynı görseli verir).</summary>
    /// <param name="sceneIndex">Sahne sırası.</param>
    /// <returns>Tohum.</returns>
    public static string Seed(int sceneIndex) => (42 + sceneIndex).ToString(CultureInfo.InvariantCulture);

    /// <summary>Görsel işareti: model, tarif ve boyutun özeti.</summary>
    /// <param name="model">Model.</param>
    /// <param name="prompt">Tarif.</param>
    /// <param name="width">Genişlik.</param>
    /// <param name="height">Yükseklik.</param>
    /// <returns>İşaret.</returns>
    public static string ImageMarker(string model, string prompt, int width, int height) =>
        Hash(string.Join('\n', model, prompt, width.ToString(CultureInfo.InvariantCulture), height.ToString(CultureInfo.InvariantCulture)));

    /// <summary>Ses işareti: model, dil, sahne süresi ve metnin özeti.</summary>
    /// <param name="model">Model.</param>
    /// <param name="language">Dil.</param>
    /// <param name="seconds">Sahne süresi.</param>
    /// <param name="narration">Metin.</param>
    /// <returns>İşaret.</returns>
    public static string VoiceMarker(string model, string language, int seconds, string narration) =>
        Hash(string.Join('\n', model, language, seconds.ToString(CultureInfo.InvariantCulture), narration));

    /// <summary>Süreç çıktısı ekran kartı/bellek hatası mı.</summary>
    /// <param name="output">Çıktı.</param>
    /// <returns>Ekran kartı kaynaklıysa <c>true</c>.</returns>
    public static bool IsGpuFailure(string? output) =>
        output is not null && GpuFailureSignatures.Any(s => output.Contains(s, StringComparison.OrdinalIgnoreCase));

    private static string Hash(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
