namespace TahaKonsept.Modules.Agents.Application.Options;

/// <summary>
/// Agents modülü ayarları (<c>Agents</c> yapılandırma bölümü): eşleme kodu süresi ve yoklama aralığı, doğrulama adresi,
/// ses önizleme zaman aşımı, heartbeat ile son görülme yazma aralığı.
/// </summary>
public sealed class AgentsOptions
{
    /// <summary>Yapılandırma bölüm adı.</summary>
    public const string SectionName = "Agents";

    /// <summary>Eşleme kodunun geçerlilik süresi (saniye).</summary>
    public int PairingLifetimeSeconds { get; set; } = 600;

    /// <summary>Ajanın token yoklama aralığı (saniye).</summary>
    public int PollIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Kullanıcının kodu onaylayacağı mutlak web adresi (ör. <c>http://tmpi.local:7110/Ayarlar#masaustu</c>).
    /// Boşsa istek yapılan sunucu adı + <see cref="WebPort"/> + <see cref="VerificationPath"/> kullanılır.
    /// </summary>
    public string? VerificationUrl { get; set; }

    /// <summary>Web uygulamasının portu (doğrulama adresi üretmek için).</summary>
    public int WebPort { get; set; } = 7110;

    /// <summary>Web'deki onay sayfası yolu.</summary>
    public string VerificationPath { get; set; } = "/Ayarlar#masaustu";

    /// <summary>Ses önizlemesi için ajan yanıtı bekleme süresi (saniye).</summary>
    public int SpeechPreviewTimeoutSeconds { get; set; } = 30;

    /// <summary>Heartbeat'te son görülmenin veritabanına en sık yazılma aralığı (saniye).</summary>
    public int LastSeenWriteIntervalSeconds { get; set; } = 30;
}
