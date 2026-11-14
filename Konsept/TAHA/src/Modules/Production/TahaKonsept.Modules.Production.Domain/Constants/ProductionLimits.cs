namespace TahaKonsept.Modules.Production.Domain.Constants;

/// <summary>
/// Production modülünün alan sınırları ve sabitleri: metin uzunlukları, deneme sayıları.
/// </summary>
public static class ProductionLimits
{
    /// <summary>Başlık azami uzunluğu.</summary>
    public const int TitleMaxLength = 300;

    /// <summary>Durum/hata mesajı azami uzunluğu.</summary>
    public const int MessageMaxLength = 2000;

    /// <summary>Hata kodu azami uzunluğu.</summary>
    public const int ErrorCodeMaxLength = 120;

    /// <summary>Hash (SHA-256 onaltılık) azami uzunluğu.</summary>
    public const int HashMaxLength = 128;

    /// <summary>Okunuş sözlüğü yazılış/okunuş azami uzunluğu.</summary>
    public const int PronunciationMaxLength = 200;

    /// <summary>Bir ajan aşamasının (geçici hatalarda) azami deneme sayısı.</summary>
    public const int MaxAgentAttempts = 3;

    /// <summary>Sahne kodu ajan doğrulamasında reddedilirse LLM'e hata ile yeniden yazdırma hakkı.</summary>
    public const int MaxSceneRepairAttempts = 1;

    /// <summary>Model/ses anahtarı azami uzunluğu (konuşma hızı ölçümleri).</summary>
    public const int ModelKeyMaxLength = 200;

    /// <summary>Konuşma hızı ölçümünde tutulan en fazla örnek ağırlığı (daha eskiler giderek etkisini yitirir).</summary>
    public const int MaxSpeechRateSamples = 20;

    /// <summary>Seslendirme tahmini profil hedefini bu oranın üstünde aşarsa işlem günlüğüne uyarı yazılır (0,15 = %15).</summary>
    public const double SpeechOverrunTolerance = 0.15;

    /// <summary>Metni kırpar (boşlukları temizler, azami uzunluğa keser).</summary>
    /// <param name="value">Metin.</param>
    /// <param name="max">Azami uzunluk.</param>
    /// <returns>Kırpılmış metin.</returns>
    public static string Clip(string? value, int max)
    {
        var text = (value ?? string.Empty).Trim();
        return text.Length <= max ? text : text[..max];
    }

    /// <summary>Boş olabilen metni kırpar.</summary>
    /// <param name="value">Metin.</param>
    /// <param name="max">Azami uzunluk.</param>
    /// <returns>Kırpılmış metin ya da <c>null</c>.</returns>
    public static string? ClipOrNull(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : Clip(value, max);
}
