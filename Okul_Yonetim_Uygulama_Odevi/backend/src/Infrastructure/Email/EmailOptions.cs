namespace OkulYonetim.Infrastructure.Email;

/// <summary>
/// E-posta ayarları ("Email" bölümü; spec 0006). Sunucu, gönderen ve web adresi girilmedikçe e-posta gönderilmez. Şifre
/// yalnızca ortam ayarından gelir; repoya, loga ve yanıta yazılmaz.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    public bool UseStartTls { get; set; } = true;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string? FromAddress { get; set; }

    public string FromName { get; set; } = "Okul Yönetim";

    /// <summary>Bağlantıların başı (ör. http://192.168.1.102:8080).</summary>
    public string? WebBaseUrl { get; set; }

    /// <summary>Yalnızca geliştirme ortamında: e-postalar bu klasöre dosya olarak yazılır.</summary>
    public string? PickupDirectory { get; set; }

    public int TimeoutSeconds { get; set; } = 10;

    public bool IsSmtpConfigured =>
        !string.IsNullOrWhiteSpace(this.Host) && !string.IsNullOrWhiteSpace(this.FromAddress) && !string.IsNullOrWhiteSpace(this.WebBaseUrl);
}
