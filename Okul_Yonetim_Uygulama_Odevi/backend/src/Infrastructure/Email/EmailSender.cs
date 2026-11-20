using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Infrastructure.Email;

/// <summary>
/// E-postayı gönderir (spec 0006 gereksinim 8). Sıra: (1) yalnızca geliştirme ortamında ve klasör ayarlıysa klasöre dosya,
/// (2) SMTP ayarlıysa sunucuya (STARTTLS, en fazla <see cref="EmailOptions.TimeoutSeconds"/> sn), (3) değilse gönderilmez,
/// yalnızca "SMTP ayarlı değil" logu (kişi kimliğiyle; adres, konu, gövde ve bağlantı loglanmaz). Hata çağırana iletilir;
/// kuyruk uyarı loglar.
/// </summary>
public sealed class EmailSender : IEmailSender
{
    private readonly IOptions<EmailOptions> options;
    private readonly IHostEnvironment environment;
    private readonly ILogger<EmailSender> logger;

    public EmailSender(IOptions<EmailOptions> options, IHostEnvironment environment, ILogger<EmailSender> logger)
    {
        this.options = options;
        this.environment = environment;
        this.logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        EmailOptions settings = this.options.Value;
        if (this.environment.IsDevelopment() && !string.IsNullOrWhiteSpace(settings.PickupDirectory))
        {
            string folder = Path.GetFullPath(settings.PickupDirectory, this.environment.ContentRootPath);
            Directory.CreateDirectory(folder);
            using SmtpClient pickup = new() { DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory, PickupDirectoryLocation = folder };
            using MailMessage mail = Compose(message, settings.FromAddress ?? "okul@localhost", settings.FromName);
            await pickup.SendMailAsync(mail, cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("E-posta geliştirme klasörüne yazıldı: kişi {UserId}", message.UserId);
            return;
        }

        if (!settings.IsSmtpConfigured)
        {
            this.logger.LogWarning("E-posta gönderilemedi: SMTP ayarlı değil (kişi {UserId})", message.UserId);
            return;
        }

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds)));
        using SmtpClient smtp = new(settings.Host, settings.Port)
        {
            EnableSsl = settings.UseStartTls,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = Math.Max(1, settings.TimeoutSeconds) * 1000,
        };
        if (!string.IsNullOrEmpty(settings.UserName))
        {
            smtp.Credentials = new NetworkCredential(settings.UserName, settings.Password);
        }

        using MailMessage smtpMail = Compose(message, settings.FromAddress!, settings.FromName);
        await smtp.SendMailAsync(smtpMail, timeout.Token).ConfigureAwait(false);
        this.logger.LogInformation("E-posta gönderildi: kişi {UserId}", message.UserId);
    }

    private static MailMessage Compose(EmailMessage message, string fromAddress, string fromName)
    {
        MailMessage mail = new(new MailAddress(fromAddress, fromName), new MailAddress(message.ToAddress, message.ToName))
        {
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = false,
            BodyEncoding = System.Text.Encoding.UTF8,
            SubjectEncoding = System.Text.Encoding.UTF8,
        };
        return mail;
    }
}
