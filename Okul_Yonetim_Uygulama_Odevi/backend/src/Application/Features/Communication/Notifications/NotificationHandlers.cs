using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication.Notifications;

/// <summary>
/// Diğer modüllerin bildirim kapısı (spec 0011 gereksinim 7, backlog B-30): yoklama gibi modüller kayıtlarını commit ettikten
/// sonra hazırlanan bildirim ve e-postaları <see cref="DeliverNotificationsCommand"/> ile buraya verir; çıkış
/// <see cref="GuardianNotifier"/>'dadır.
/// </summary>
public sealed class NotificationHandlers : IRequestHandler<DeliverNotificationsCommand, int>
{
    private readonly GuardianNotifier notifier;

    public NotificationHandlers(GuardianNotifier notifier)
    {
        this.notifier = notifier;
    }

    public Task<int> Handle(DeliverNotificationsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.notifier.DeliverAsync(request.Pushes, request.Emails, cancellationToken);
    }
}
