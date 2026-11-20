namespace OkulYonetim.Application.Ports;

/// <summary>Kullanıcıya gönderilen bağlantıların adresi (web adresi ayardan).</summary>
public interface IAppLinks
{
    /// <summary>Yeni şifre belirleme sayfası; anahtar adresin "#" sonrasında (sunucuya ve loglara gitmez).</summary>
    string PasswordReset(string token);
}
