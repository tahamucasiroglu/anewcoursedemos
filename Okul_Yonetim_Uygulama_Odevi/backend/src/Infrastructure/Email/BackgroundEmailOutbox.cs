using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Infrastructure.Email;

/// <summary>
/// Bellekte e-posta kuyruğu + arka plan gönderici (spec 0006): istek e-postayı sıraya koyup hemen döner; gönderim hatası
/// isteği etkilemez, uyarı loglanır (adres, gövde, bağlantı loglanmaz). Uygulama yeniden başlarsa bekleyen e-posta kaybolur;
/// kişi yeniden ister (plan Risks 2). Kuyruk dolarsa (1000) yeni e-posta bırakılır ve uyarı loglanır.
/// </summary>
public sealed class BackgroundEmailOutbox : BackgroundService, IEmailOutbox
{
    private readonly Channel<EmailMessage> channel = Channel.CreateBounded<EmailMessage>(new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropWrite });
    private readonly IEmailSender sender;
    private readonly ILogger<BackgroundEmailOutbox> logger;

    public BackgroundEmailOutbox(IEmailSender sender, ILogger<BackgroundEmailOutbox> logger)
    {
        this.sender = sender;
        this.logger = logger;
    }

    public void Enqueue(EmailMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!this.channel.Writer.TryWrite(message))
        {
            this.logger.LogWarning("E-posta kuyruğu dolu, e-posta bırakıldı: kişi {UserId}", message.UserId);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (EmailMessage message in this.channel.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await this.sender.SendAsync(message, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                this.logger.LogWarning("E-posta gönderilemedi: kişi {UserId} ({ErrorType})", message.UserId, exception.GetType().Name);
            }
        }
    }
}
