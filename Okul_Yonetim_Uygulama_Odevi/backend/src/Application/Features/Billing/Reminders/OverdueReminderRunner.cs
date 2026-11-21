using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Identity.Sessions;

namespace OkulYonetim.Application.Features.Billing.Reminders;

/// <summary>
/// Bir okulun hatırlatma çalışması (spec 0015 gereksinim 9; plan 0015 Risks 2): arka plan işçisi her okul için yeni bir DI
/// kapsamı açar ve bunu çağırır. Kapsamın oturumu (<see cref="CurrentSession"/>) okul için sistem oturumu yapılır (kişi yok, okul seçili,
/// bütün şubeler) ve <see cref="SendOverdueRemindersCommand"/> gönderilir — kurum filtresi bu okulla sınırlı kalır, yeni
/// <c>IgnoreQueryFilters</c> yok. Oturumu zaten dolu bir kapsamda (bir HTTP isteğinde) kullanılamaz.
/// </summary>
public sealed class OverdueReminderRunner
{
    private readonly CurrentSession session;
    private readonly ISender sender;

    public OverdueReminderRunner(CurrentSession session, ISender sender)
    {
        this.session = session;
        this.sender = sender;
    }

    public Task<int> RunAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (this.session.IsAuthenticated || (this.session.TenantId is { } current && current != tenantId))
        {
            throw new InvalidOperationException("Sistem oturumu yalnızca boş bir arka plan kapsamında açılır.");
        }

        this.session.AuthenticateSystem(tenantId);
        return this.sender.Send(new SendOverdueRemindersCommand(), cancellationToken);
    }
}
