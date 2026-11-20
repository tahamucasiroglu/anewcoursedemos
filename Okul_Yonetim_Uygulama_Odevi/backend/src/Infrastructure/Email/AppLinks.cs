using Microsoft.Extensions.Options;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Infrastructure.Email;

/// <summary>Kullanıcıya gönderilen bağlantılar: web adresi ayardan; anahtar "#" sonrasında (sunucuya gitmez).</summary>
public sealed class AppLinks : IAppLinks
{
    private readonly IOptions<EmailOptions> options;

    public AppLinks(IOptions<EmailOptions> options)
    {
        this.options = options;
    }

    public string PasswordReset(string token)
    {
        string baseUrl = (this.options.Value.WebBaseUrl ?? string.Empty).TrimEnd('/');
        return $"{baseUrl}/#/sifre-yenile?anahtar={Uri.EscapeDataString(token)}";
    }
}
