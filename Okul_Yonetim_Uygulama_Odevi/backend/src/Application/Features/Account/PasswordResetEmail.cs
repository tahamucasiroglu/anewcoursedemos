using OkulYonetim.Application.Ports;

namespace OkulYonetim.Application.Features.Account;

/// <summary>Şifre yenileme e-postası (spec 0006 gereksinim 8): Türkçe konu ve gövde.</summary>
public static class PasswordResetEmail
{
    public static EmailMessage Build(Guid userId, string toAddress, string fullName, string subject, string link)
    {
        string body =
            $"Merhaba {fullName},\n\n" +
            "Okul Yönetim hesabınız için şifre yenileme isteği aldık. Yeni şifrenizi belirlemek için bağlantıyı açın:\n\n" +
            $"{link}\n\n" +
            "Bu bağlantı 30 dakika geçerlidir ve bir kez kullanılabilir.\n\n" +
            "Bu isteği siz yapmadıysanız bu e-postayı yok sayın; şifreniz değişmez.\n";
        return new EmailMessage(toAddress, fullName, subject, body, userId);
    }
}
