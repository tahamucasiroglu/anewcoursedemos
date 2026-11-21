using Microsoft.Extensions.Logging;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Communication;

namespace OkulYonetim.Application.Features.Communication.Notifications;

/// <summary>
/// Diğer modüllerin bildirim kapısı (spec 0011 gereksinim 7, backlog B-30): yoklama gibi modüller kayıtlarını commit ettikten
/// sonra hazırlanan bildirim ve e-postaları <see cref="DeliverNotificationsCommand"/> ile buraya verir; çıkış
/// <see cref="GuardianNotifier"/>'dadır. Spec 0016: öğretmene tek bildirim <see cref="NotifyTeacherCommand"/> ile — mesai saatleri
/// (Türkiye saati, seçili gün, başlangıç dahil – bitiş hariç) burada denetlenir.
/// </summary>
public sealed class NotificationHandlers :
    IRequestHandler<DeliverNotificationsCommand, int>,
    IRequestHandler<NotifyTeacherCommand, bool>
{
    private readonly GuardianNotifier notifier;
    private readonly ICurrentSession session;
    private readonly IQuietHoursRepository quietHours;
    private readonly SchoolCalendar calendar;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<NotificationHandlers> logger;

    public NotificationHandlers(
        GuardianNotifier notifier,
        ICurrentSession session,
        IQuietHoursRepository quietHours,
        SchoolCalendar calendar,
        TimeProvider timeProvider,
        ILogger<NotificationHandlers> logger)
    {
        this.notifier = notifier;
        this.session = session;
        this.quietHours = quietHours;
        this.calendar = calendar;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public Task<int> Handle(DeliverNotificationsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.notifier.DeliverAsync(request.Pushes, request.Emails, cancellationToken);
    }

    public async Task<bool> Handle(NotifyTeacherCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Guid tenantId = this.session.TenantId ?? throw new InvalidOperationException("Bildirim için seçili okul gerekli.");
        if (await this.quietHours.FindAsync(request.TeacherUserId, cancellationToken).ConfigureAwait(false) is QuietHours hours)
        {
            DateTimeOffset local = this.calendar.TurkeyTime(this.timeProvider.GetUtcNow());
            if (!hours.IsWorkingTime(local.DayOfWeek, TimeOnly.FromDateTime(local.DateTime)))
            {
                this.logger.LogInformation(
                    "Öğretmen bildirimi mesai dışında gönderilmedi: okul {TenantId}, tür {Kind}, kişi {UserId}, kaynak {SourceId}",
                    tenantId,
                    request.Kind,
                    request.TeacherUserId,
                    request.SourceId);
                return false;
            }
        }

        await this.notifier.DeliverAsync([new PushNotification(tenantId, request.TeacherUserId, request.Kind, request.SourceId, request.Text)], [], cancellationToken).ConfigureAwait(false);
        return true;
    }
}
