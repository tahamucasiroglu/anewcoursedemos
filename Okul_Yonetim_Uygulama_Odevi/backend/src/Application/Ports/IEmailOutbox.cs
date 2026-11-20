namespace OkulYonetim.Application.Ports;

/// <summary>
/// E-postayı arka planda gönderilmek üzere sıraya koyar ve hemen döner (spec 0006: istek süresi e-postanın varlığını ele
/// vermez; SMTP yavaşsa ya da yoksa istek beklemez).
/// </summary>
public interface IEmailOutbox
{
    void Enqueue(EmailMessage message);
}
