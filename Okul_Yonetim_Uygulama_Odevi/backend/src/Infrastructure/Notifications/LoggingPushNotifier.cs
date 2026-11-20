using Microsoft.Extensions.Logging;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Infrastructure.Notifications;

/// <summary>
/// Sahte bildirim göndericisi (spec 0011 C-7): gerçek push sağlayıcısı yok (B-6), bildirim yalnızca kimliklerle Information
/// olarak loglanır — metin, ad ve iletişim bilgisi yok. Gerçek gönderici geldiğinde yalnızca bu kayıt değişir.
/// </summary>
public sealed class LoggingPushNotifier : IPushNotifier
{
    private readonly ILogger<LoggingPushNotifier> logger;

    public LoggingPushNotifier(ILogger<LoggingPushNotifier> logger)
    {
        this.logger = logger;
    }

    public Task NotifyAsync(PushNotification notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        this.logger.LogInformation(
            "Bildirim {Kind} → kişi {UserId} (okul {TenantId}, kaynak {SourceId})",
            notification.Kind,
            notification.RecipientUserId,
            notification.TenantId,
            notification.SourceId);
        return Task.CompletedTask;
    }
}
