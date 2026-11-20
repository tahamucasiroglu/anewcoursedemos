namespace OkulYonetim.Application.Ports;

/// <summary>E-postayı gerçekten gönderir (SMTP, geliştirmede klasör ya da "ayarlı değil" logu).</summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
