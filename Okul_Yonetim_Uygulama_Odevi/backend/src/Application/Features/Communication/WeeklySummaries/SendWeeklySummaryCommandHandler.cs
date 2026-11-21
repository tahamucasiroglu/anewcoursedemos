using System.Globalization;
using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Administration;
using OkulYonetim.Application.Features.Ai.WeeklySummaries;
using OkulYonetim.Application.Features.Communication.Notifications;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Ai;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Domain.Compliance;

namespace OkulYonetim.Application.Features.Communication.WeeklySummaries;

/// <summary>
/// Haftalık gelişim özetinin veliye gönderimi (spec 0014 gereksinim 5, 9; BR-5, BR-6) — veliye giden her şeyin tek çıkışı
/// Communication'dır; Ai yalnızca taslak üretir ve onay kapısını sunar (mimari yasak 6). Sıra: öğrenci-hafta sırası
/// (<see cref="WeeklySummaryLock"/>; dolu kalırsa "Bu özet şu an işleniyor…") → Ai'ın <see cref="GetWeeklySummaryForSendQuery"/>'si
/// (öğretmen öğrencinin sınıfına atanmış — değilse 403/404; bekleyen taslak yoksa 404; gönderilmişse "Bu hafta için özet zaten
/// gönderildi."; bekleyen taslak öğretmenin panelde gördüğü değilse — hafta döndü ya da yeniden oluşturuldu — "Taslak değişti; paneli
/// yenileyin.", review R1-2) → <see cref="NotifyStudentGuardiansCommand"/> (öğrencinin okulda etkin her velisine bir okul bildirimi; metin = onaylanan
/// özet + "Yapay zekâ desteğiyle hazırlandı, öğretmen onayladı." + dayanak satırı; etkin veli yoksa "Bu öğrencinin kayıtlı velisi yok." ve
/// hiçbir şey yazılmaz) → Ai'ın <see cref="ApproveWeeklySummaryDraftCommand"/>'ı (son metin, onaylayan) → audit (kim, öğrenci, hafta, veli
/// sayısı, IP) — hepsi tek işlemde; bildirimler commit'ten sonra bildirim kapısından (<see cref="DeliverNotificationsCommand"/>). Eşzamanlı
/// iki gönderimde ikincisi sırayı bekler ve onaylanmış taslağı görür: her veliye tek özet. Loglarda yalnızca kimlik ve sayı.
/// </summary>
public sealed class SendWeeklySummaryCommandHandler : IRequestHandler<SendWeeklySummaryCommand, WeeklySummarySent>
{
    private readonly ISender sender;
    private readonly SchoolCalendar calendar;
    private readonly AdminAudit audit;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<SendWeeklySummaryCommandHandler> logger;

    public SendWeeklySummaryCommandHandler(
        ISender sender,
        SchoolCalendar calendar,
        AdminAudit audit,
        IUnitOfWork unitOfWork,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<SendWeeklySummaryCommandHandler> logger)
    {
        this.sender = sender;
        this.calendar = calendar;
        this.audit = audit;
        this.unitOfWork = unitOfWork;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<WeeklySummarySent> Handle(SendWeeklySummaryCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        SchoolWeek week = SchoolWeek.Choose(request.Week, this.calendar.Today());
        string finalText = request.Text!.Trim();
        IExclusiveScope scope;
        try
        {
            scope = await this.unitOfWork.BeginExclusiveAsync(WeeklySummaryLock.Key(request.StudentId, week.Start), cancellationToken).ConfigureAwait(false);
        }
        catch (ExclusiveLockTimeoutException)
        {
            this.logger.LogWarning("Haftalık özet sırası dolu (gönderim): öğrenci {StudentId}, hafta {WeekStart}", request.StudentId, week.Start);
            throw new BusinessException(await this.localization.CommunicationTextAsync(CommunicationMessages.WeeklySummaryBusy).ConfigureAwait(false));
        }

        WeeklySummaryForSend target;
        SchoolNoticeResult notices;
        await using (scope.ConfigureAwait(false))
        {
            target = await this.sender.Send(new GetWeeklySummaryForSendQuery(request.StudentId, week.Start, request.DraftId!.Value, request.DraftGeneratedAt!.Value), cancellationToken).ConfigureAwait(false);
            string label = await this.localization.CommunicationTextAsync(CommunicationMessages.WeeklySummaryLabel).ConfigureAwait(false);
            string text = finalText + "\n\n" + label + "\n" + target.BasisLine;
            notices = await this.sender.Send(
                new NotifyStudentGuardiansCommand([target.StudentId], GuardianNoticeKind.WeeklySummary, target.DraftId, week.Start, text),
                cancellationToken).ConfigureAwait(false);
            if (notices.NotifiedGuardians == 0)
            {
                this.logger.LogWarning("Haftalık özet gönderilmedi (etkin veli yok): öğrenci {StudentId}, hafta {WeekStart}, öğretmen {UserId}", target.StudentId, week.Start, target.TeacherUserId);
                throw new BusinessException(await this.localization.CommunicationTextAsync(CommunicationMessages.WeeklySummaryNoGuardian).ConfigureAwait(false));
            }

            await this.sender.Send(new ApproveWeeklySummaryDraftCommand(target.DraftId, finalText), cancellationToken).ConfigureAwait(false);
            this.audit.Record(
                AuditAction.WeeklySummarySent,
                target.TenantId,
                target.TeacherUserId,
                nameof(AiDraft),
                target.DraftId,
                string.Create(CultureInfo.InvariantCulture, $"student={target.StudentId:D};week={week.Start:yyyy-MM-dd};guardians={notices.NotifiedGuardians}"));
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        // Kayıt kalıcı olduktan sonra bildirim kapısına: veliye giden her şey tek çıkıştan geçer (spec 0011).
        await this.sender.Send(new DeliverNotificationsCommand(notices.Pushes, []), cancellationToken).ConfigureAwait(false);
        this.logger.LogInformation(
            "Haftalık özet gönderildi: taslak {DraftId}, öğrenci {StudentId}, hafta {WeekStart}, öğretmen {UserId}, bildirim giden veli {Notified}",
            target.DraftId,
            target.StudentId,
            week.Start,
            target.TeacherUserId,
            notices.NotifiedGuardians);
        return new WeeklySummarySent(target.DraftId, notices.NotifiedGuardians, this.timeProvider.GetUtcNow());
    }
}
