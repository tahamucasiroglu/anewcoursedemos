using Microsoft.Extensions.Logging;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Communication;
using OkulYonetim.Application.Features.Communication.Notifications;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Features.Students.Queries.ListStudentLabels;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Billing;
using OkulYonetim.Domain.Communication;

namespace OkulYonetim.Application.Features.Billing.Reminders;

/// <summary>
/// Bir okulun gecikme hatırlatmaları (spec 0015 gereksinim 9, C-7; plan Risks 3). Okulun hatırlatma sırasında
/// (<c>billing-reminders:&lt;okul&gt;</c>): geçerli planların vadesi geçmiş ve kalanı olan taksitleri taze okunur, her biri için
/// <see cref="ReminderSchedule.Due"/> (Türkiye saati) gönderilecek sırayı verirse ve öğrenci ayrılmamışsa önce tekil
/// <see cref="PaymentReminder"/> (taksit + sıra) yazılır, aynı işlemde Communication'ın <see cref="NotifyStudentGuardiansCommand"/>'ı
/// öğrencinin okulda etkin her velisine tek "Okul bildirimi" ekler; commit sonrası bildirimler kapıdan çıkar
/// (<see cref="DeliverNotificationsCommand"/> — gönderici hatası kapıda Warning, diğer hatırlatmalar sürer, hatırlatma gönderilmiş
/// sayılır). Aynı anda iki çalışma: ikincisi sırayı bekler ve gönderilmiş kayıtları görür — tek bildirim. Ödenmiş taksit, iptal plan
/// ve ayrılmış öğrenci aday olmaz. Her gönderim Information log'u (yalnızca kimlikler; metin ve tutar loglanmaz).
/// </summary>
public sealed class SendOverdueRemindersCommandHandler : IRequestHandler<SendOverdueRemindersCommand, int>
{
    private readonly ICurrentSession session;
    private readonly IBillingRepository billing;
    private readonly ISender sender;
    private readonly SchoolCalendar calendar;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<SendOverdueRemindersCommandHandler> logger;

    public SendOverdueRemindersCommandHandler(
        ICurrentSession session,
        IBillingRepository billing,
        ISender sender,
        SchoolCalendar calendar,
        IUnitOfWork unitOfWork,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<SendOverdueRemindersCommandHandler> logger)
    {
        this.session = session;
        this.billing = billing;
        this.sender = sender;
        this.calendar = calendar;
        this.unitOfWork = unitOfWork;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<int> Handle(SendOverdueRemindersCommand request, CancellationToken cancellationToken)
    {
        Guid tenantId = this.session.TenantId ?? throw new InvalidOperationException("Hatırlatma için seçili okul gerekli.");
        DateTimeOffset now = this.timeProvider.GetUtcNow();
        if (this.calendar.TurkeyTime(now).Hour < ReminderSchedule.SendHour)
        {
            return 0;
        }

        IExclusiveScope scope;
        try
        {
            scope = await this.unitOfWork.BeginExclusiveAsync("billing-reminders:" + tenantId.ToString("N"), cancellationToken).ConfigureAwait(false);
        }
        catch (ExclusiveLockTimeoutException)
        {
            this.logger.LogWarning("Hatırlatma sırası alınamadı (başka çalışma sürüyor): okul {TenantId}", tenantId);
            return 0;
        }

        List<PushNotification> pushes = [];
        int sent = 0;
        await using (scope.ConfigureAwait(false))
        {
            now = this.timeProvider.GetUtcNow();
            DateTimeOffset turkeyNow = this.calendar.TurkeyTime(now);
            DateOnly today = this.calendar.Today();
            IReadOnlyList<ReminderCandidate> candidates = await this.billing.ListReminderCandidatesAsync(today, cancellationToken).ConfigureAwait(false);
            List<(ReminderCandidate Candidate, int Sequence)> due = [.. candidates
                .Select(c => (Candidate: c, Sequence: ReminderSchedule.Due(c.Installment.DueDate, turkeyNow, c.LastSentSequence)))
                .Where(x => x.Sequence is not null)
                .Select(x => (x.Candidate, x.Sequence!.Value))];
            if (due.Count == 0)
            {
                return 0;
            }

            Dictionary<Guid, StudentLabel> students = (await this.sender.Send(new ListStudentLabelsQuery([.. due.Select(d => d.Candidate.StudentId).Distinct()]), cancellationToken).ConfigureAwait(false))
                .Where(s => s.IsActive)
                .ToDictionary(s => s.Id);
            string template = await this.localization.BillingTextAsync(BillingMessages.ReminderNotice).ConfigureAwait(false);
            List<(PaymentReminder Reminder, Guid StudentId, int Guardians)> written = [];
            foreach ((ReminderCandidate candidate, int sequence) in due)
            {
                if (!students.TryGetValue(candidate.StudentId, out StudentLabel? student))
                {
                    continue;
                }

                PaymentReminder reminder = PaymentReminder.Sent(candidate.Installment, sequence, now);
                this.billing.AddReminder(reminder);
                string text = ReminderNoticeText.Render(template, student.FullName, candidate.Installment.DueDate, candidate.Installment.Remaining);
                SchoolNoticeResult notices = await this.sender.Send(
                    new NotifyStudentGuardiansCommand([student.Id], GuardianNoticeKind.PaymentReminder, reminder.Id, today, text),
                    cancellationToken).ConfigureAwait(false);
                pushes.AddRange(notices.Pushes);
                written.Add((reminder, student.Id, notices.NotifiedGuardians));
            }

            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            foreach ((PaymentReminder reminder, Guid studentId, int guardians) in written)
            {
                this.logger.LogInformation(
                    "Gecikme hatırlatması gönderildi: okul {TenantId}, öğrenci {StudentId}, taksit {InstallmentId}, sıra {Sequence}, veli {GuardianCount}",
                    tenantId,
                    studentId,
                    reminder.InstallmentId,
                    reminder.Sequence,
                    guardians);
            }

            sent = written.Count;
        }

        // Kayıt kalıcı olduktan sonra bildirim kapısına: veliye giden her şey tek çıkıştan geçer (spec 0011).
        if (pushes.Count > 0)
        {
            await this.sender.Send(new DeliverNotificationsCommand(pushes, []), cancellationToken).ConfigureAwait(false);
        }

        return sent;
    }
}
