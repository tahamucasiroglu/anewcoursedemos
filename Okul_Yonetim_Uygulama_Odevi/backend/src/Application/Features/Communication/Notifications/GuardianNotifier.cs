using Microsoft.Extensions.Logging;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Application.Features.Communication.Notifications;

/// <summary>
/// Bildirim kapısı — iletişimin tek çıkışı (spec 0011 gereksinim 7; mimari "Communication … veliye giden her şeyin tek
/// çıkışı"). Duyuru, onay formu, mesaj ve diğer modüllerin okul bildirimleri (yoklama) buradan çıkar: kayıt kalıcı olduktan
/// <b>sonra</b> her bildirim <see cref="IPushNotifier"/>'a verilir ve e-postalar <see cref="IEmailOutbox"/> kuyruğuna bırakılır
/// (geri alınan işlem bildirim/e-posta göndermez; backlog B-30). Gönderici hata verirse asıl işlem başarılı kalır; hata
/// yalnızca kimliklerle Warning olarak loglanır ve sıradaki bildirime geçilir.
/// </summary>
public sealed class GuardianNotifier
{
    private readonly IPushNotifier pushNotifier;
    private readonly IEmailOutbox emailOutbox;
    private readonly ILogger<GuardianNotifier> logger;

    public GuardianNotifier(IPushNotifier pushNotifier, IEmailOutbox emailOutbox, ILogger<GuardianNotifier> logger)
    {
        this.pushNotifier = pushNotifier;
        this.emailOutbox = emailOutbox;
        this.logger = logger;
    }

    /// <summary>Bildirimleri gönderir, e-postaları kuyruğa bırakır; gönderilebilen bildirim sayısını döndürür.</summary>
    public async Task<int> DeliverAsync(IReadOnlyCollection<PushNotification> pushes, IReadOnlyCollection<EmailMessage> emails, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pushes);
        ArgumentNullException.ThrowIfNull(emails);
        int delivered = 0;
        foreach (PushNotification push in pushes)
        {
            try
            {
                await this.pushNotifier.NotifyAsync(push, cancellationToken).ConfigureAwait(false);
                delivered++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException and not OutOfMemoryException)
            {
                this.logger.LogWarning(
                    "Bildirim gönderilemedi ({ErrorType}): okul {TenantId}, tür {Kind}, kişi {UserId}, kaynak {SourceId}",
                    exception.GetType().Name,
                    push.TenantId,
                    push.Kind,
                    push.RecipientUserId,
                    push.SourceId);
            }
        }

        foreach (EmailMessage email in emails)
        {
            this.emailOutbox.Enqueue(email);
        }

        return delivered;
    }
}
