using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Ai.Prompts;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Features.Organization.Queries.ListMyTeachingClassrooms;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Features.Students.Queries.ListClassroomRoster;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Ai;

namespace OkulYonetim.Application.Features.Ai.WeeklySummaries;

/// <summary>
/// Haftalık gelişim özeti — Ai tarafı (spec 0014): sınıflar, sınıf + hafta durum listesi, öğrenci paneli (dayanak + taslak), "Özet
/// oluştur", "Vazgeç" ve Communication'ın onay kapısı (bekleyen taslak + onay). Yetki her istekte <see cref="WeeklySummaryGuard"/> ile
/// (öğretmen değilse 403, sınıf/öğrenci erişimi yoksa 404). Hafta Türkiye günüyle (<see cref="SchoolCalendar"/>, <see cref="SchoolWeek"/>).
/// <para>
/// Üretim: zaten gönderilmiş hafta "Bu hafta için özet zaten gönderildi."; dayanak (yoklama, ödev/not, öğretmen notu) boşsa "Bu hafta
/// için özetlenecek veri yok." ve model çağrılmaz; metin servisine yalnızca sabit talimat + dayanağın metni gider (ad, soyad değil —
/// <see cref="WeeklySummarySource"/>); servis aksaklığı ya da boş yanıt Warning loglanır, "Yapay zekâ şu an kullanılamıyor. Daha sonra
/// tekrar deneyin." döner ve var olan bekleyen taslağa dokunulmaz. Kayıt öğrenci-hafta sırasında (<see cref="WeeklySummaryLock"/>):
/// bekleyen taslak yeniden oluşturulur (tek taslak), yoksa eklenir. Ai veliye hiçbir şey göndermez (BR-5; mimari yasak 6) —
/// gönderim Communication'ın komutudur. Loglarda yalnızca kimlikler, karakter sayıları ve süreler; özet, not, dayanak ve ad yok.
/// </para>
/// </summary>
public sealed class WeeklySummaryHandlers :
    IRequestHandler<ListWeeklySummaryClassroomsQuery, IReadOnlyList<WeeklySummaryClassroom>>,
    IRequestHandler<ListWeeklySummaryStatusQuery, WeeklySummaryList>,
    IRequestHandler<GetWeeklySummaryPanelQuery, WeeklySummaryPanel>,
    IRequestHandler<GenerateWeeklySummaryDraftCommand, WeeklySummaryPanel>,
    IRequestHandler<DiscardWeeklySummaryDraftCommand, bool>,
    IRequestHandler<GetWeeklySummaryForSendQuery, WeeklySummaryForSend>,
    IRequestHandler<ApproveWeeklySummaryDraftCommand, bool>
{
    public const string StatusNone = "None";
    public const string StatusDraft = "Draft";
    public const string StatusSent = "Sent";

    private static readonly StringComparer TurkishOrder = StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true);

    private readonly WeeklySummaryGuard guard;
    private readonly WeeklySummarySourceCollector collector;
    private readonly IAiTextGenerator generator;
    private readonly IAiDraftRepository drafts;
    private readonly ICurrentSession session;
    private readonly SchoolCalendar calendar;
    private readonly ISender sender;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<WeeklySummaryHandlers> logger;

    public WeeklySummaryHandlers(
        WeeklySummaryGuard guard,
        WeeklySummarySourceCollector collector,
        IAiTextGenerator generator,
        IAiDraftRepository drafts,
        ICurrentSession session,
        SchoolCalendar calendar,
        ISender sender,
        IUnitOfWork unitOfWork,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<WeeklySummaryHandlers> logger)
    {
        this.guard = guard;
        this.collector = collector;
        this.generator = generator;
        this.drafts = drafts;
        this.session = session;
        this.calendar = calendar;
        this.sender = sender;
        this.unitOfWork = unitOfWork;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<IReadOnlyList<WeeklySummaryClassroom>> Handle(ListWeeklySummaryClassroomsQuery request, CancellationToken cancellationToken)
    {
        await this.guard.RequireTeacherAsync(cancellationToken).ConfigureAwait(false);
        DateOnly since = SchoolWeek.Containing(this.calendar.Today()).Before().Start;
        IReadOnlyList<TeacherClassroomRow> rows = await this.sender.Send(new ListMyTeachingClassroomsQuery(since), cancellationToken).ConfigureAwait(false);
        return [.. rows
            .OrderBy(c => c.BranchName, TurkishOrder).ThenBy(c => c.Name, TurkishOrder).ThenBy(c => c.ClassroomId)
            .Select(c => new WeeklySummaryClassroom(c.ClassroomId, c.Name, c.BranchName))];
    }

    public async Task<WeeklySummaryList> Handle(ListWeeklySummaryStatusQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (_, Guid userId, IReadOnlySet<Guid> branchIds) = await this.guard.RequireTeacherAsync(cancellationToken).ConfigureAwait(false);
        ClassroomSummary classroom = await this.guard.RequireClassroomAsync(userId, branchIds, request.ClassroomId, cancellationToken).ConfigureAwait(false);
        SchoolWeek week = this.Week(request.Week);
        IReadOnlyList<RosterStudent> roster = await this.sender.Send(new ListClassroomRosterQuery(classroom.Id), cancellationToken).ConfigureAwait(false);
        Dictionary<Guid, AiDraftStatus> statuses = roster.Count == 0
            ? []
            : (await this.drafts.ListWeeklySummaryStatusesAsync([.. roster.Select(s => s.Id)], week.Start, cancellationToken).ConfigureAwait(false))
                .ToDictionary(s => s.StudentId, s => s.Status);
        return new WeeklySummaryList(
            classroom.Id,
            classroom.Name,
            week.Start,
            week.End,
            week.Label(),
            [.. roster.Select(s => new WeeklySummaryStudentRow(s.Id, s.FullName, StatusOf(statuses.TryGetValue(s.Id, out AiDraftStatus status) ? status : null)))]);
    }

    public async Task<WeeklySummaryPanel> Handle(GetWeeklySummaryPanelQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        WeeklySummaryStudent student = await this.guard.RequireStudentAsync(request.StudentId, cancellationToken).ConfigureAwait(false);
        SchoolWeek week = this.Week(request.Week);
        AiDraft? draft = await this.drafts.FindWeeklySummaryAsync(student.StudentId, week.Start, cancellationToken).ConfigureAwait(false);
        WeeklySummarySource basis = draft is not null
            ? WeeklySummarySource.FromJson(draft.SourceText)
            : await this.collector.CollectAsync(student, week, null, cancellationToken).ConfigureAwait(false);
        return await this.PanelAsync(student, week, basis, draft).ConfigureAwait(false);
    }

    public async Task<WeeklySummaryPanel> Handle(GenerateWeeklySummaryDraftCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        WeeklySummaryStudent student = await this.guard.RequireStudentAsync(request.StudentId, cancellationToken).ConfigureAwait(false);
        SchoolWeek week = this.Week(request.Week);
        IReadOnlyList<(Guid StudentId, AiDraftStatus Status)> current =
            await this.drafts.ListWeeklySummaryStatusesAsync([student.StudentId], week.Start, cancellationToken).ConfigureAwait(false);
        if (current.Any(c => c.Status == AiDraftStatus.Approved))
        {
            throw await this.AlreadySentAsync(student, week).ConfigureAwait(false);
        }

        WeeklySummarySource source = await this.collector.CollectAsync(student, week, request.TeacherNote, cancellationToken).ConfigureAwait(false);
        if (source.IsEmpty)
        {
            this.logger.LogWarning("Haftalık özet üretilmedi (veri yok): öğrenci {StudentId}, hafta {WeekStart}, öğretmen {UserId}", student.StudentId, week.Start, student.TeacherUserId);
            throw new BusinessException(await this.localization.AiTextAsync(AiMessages.SummaryNoData).ConfigureAwait(false));
        }

        Stopwatch watch = Stopwatch.StartNew();
        string text = await this.GenerateAsync(source, student, week, cancellationToken).ConfigureAwait(false);
        long textMs = watch.ElapsedMilliseconds;

        IExclusiveScope scope = await this.LockAsync(student.StudentId, week, cancellationToken).ConfigureAwait(false);
        AiDraft draft;
        await using (scope.ConfigureAwait(false))
        {
            AiDraft? existing = await this.drafts.FindWeeklySummaryAsync(student.StudentId, week.Start, cancellationToken).ConfigureAwait(false);
            DateTimeOffset now = this.timeProvider.GetUtcNow();
            if (existing is { IsPending: false })
            {
                throw await this.AlreadySentAsync(student, week).ConfigureAwait(false);
            }

            if (existing is null)
            {
                draft = AiDraft.WeeklySummary(student.TenantId, student.TeacherUserId, student.StudentId, week.Start, source.ToJson(), text, now);
                this.drafts.Add(draft);
            }
            else
            {
                draft = existing;
                draft.Regenerate(student.TeacherUserId, source.ToJson(), text, now);
            }

            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        this.logger.LogInformation(
            "Haftalık özet taslağı oluşturuldu: taslak {DraftId}, öğrenci {StudentId}, hafta {WeekStart}, öğretmen {UserId}, yeniden {Regenerated}, taslak {DraftLength} karakter, metin {TextMs} ms",
            draft.Id,
            student.StudentId,
            week.Start,
            student.TeacherUserId,
            current.Count > 0,
            draft.DraftText.Length,
            textMs);
        return await this.PanelAsync(student, week, source, draft).ConfigureAwait(false);
    }

    public async Task<bool> Handle(DiscardWeeklySummaryDraftCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        WeeklySummaryStudent student = await this.guard.RequireStudentAsync(request.StudentId, cancellationToken).ConfigureAwait(false);
        SchoolWeek week = this.Week(request.Week);
        IExclusiveScope scope = await this.LockAsync(student.StudentId, week, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            AiDraft draft = await this.drafts.FindWeeklySummaryAsync(student.StudentId, week.Start, cancellationToken).ConfigureAwait(false) is { IsPending: true } pending
                ? pending
                : throw await this.guard.NotFoundAsync(AiMessages.DraftNotFound).ConfigureAwait(false);
            this.drafts.Remove(draft);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Haftalık özet taslağından vazgeçildi (silindi): taslak {DraftId}, öğrenci {StudentId}, hafta {WeekStart}, öğretmen {UserId}", draft.Id, student.StudentId, week.Start, student.TeacherUserId);
            return true;
        }
    }

    public async Task<WeeklySummaryForSend> Handle(GetWeeklySummaryForSendQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        WeeklySummaryStudent student = await this.guard.RequireStudentAsync(request.StudentId, cancellationToken).ConfigureAwait(false);
        SchoolWeek week = SchoolWeek.StartingOn(request.WeekStart);
        AiDraft draft = await this.drafts.FindWeeklySummaryAsync(student.StudentId, week.Start, cancellationToken).ConfigureAwait(false)
            ?? throw await this.guard.NotFoundAsync(AiMessages.DraftNotFound).ConfigureAwait(false);
        if (!draft.IsPending)
        {
            throw await this.AlreadySentAsync(student, week).ConfigureAwait(false);
        }

        string basisLine = WeeklySummarySource.FromJson(draft.SourceText).ToBasisLine(await this.BasisTextsAsync().ConfigureAwait(false));
        return new WeeklySummaryForSend(student.TenantId, student.TeacherUserId, draft.Id, student.StudentId, week.Start, basisLine);
    }

    public async Task<bool> Handle(ApproveWeeklySummaryDraftCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        AiDraft draft = await this.drafts.FindAsync(request.DraftId, cancellationToken).ConfigureAwait(false) is { Kind: AiDraftKind.WeeklySummary } found
            ? found
            : throw await this.guard.NotFoundAsync(AiMessages.DraftNotFound).ConfigureAwait(false);
        WeeklySummaryStudent student = await this.guard.RequireStudentAsync(draft.StudentId, cancellationToken).ConfigureAwait(false);
        if (!draft.IsPending)
        {
            throw await this.AlreadySentAsync(student, SchoolWeek.StartingOn(draft.PeriodStart!.Value)).ConfigureAwait(false);
        }

        Guid approver = this.session.UserId ?? throw new InvalidOperationException("Onay için oturum gerekli.");
        draft.Approve(request.FinalText, approver, null, this.timeProvider.GetUtcNow());
        this.logger.LogInformation(
            "Haftalık özet onaylandı: taslak {DraftId}, öğrenci {StudentId}, hafta {WeekStart}, onaylayan {UserId}, son metin {FinalLength} karakter, düzenlendi {Edited}",
            draft.Id,
            draft.StudentId,
            draft.PeriodStart,
            approver,
            request.FinalText.Length,
            !string.Equals(draft.DraftText, request.FinalText, StringComparison.Ordinal));
        return true;
    }

    private static string StatusOf(AiDraftStatus? status) => status switch
    {
        AiDraftStatus.Pending => StatusDraft,
        AiDraftStatus.Approved => StatusSent,
        _ => StatusNone,
    };

    private SchoolWeek Week(string? choice) => SchoolWeek.Choose(choice, this.calendar.Today());

    private async Task<WeeklySummaryPanel> PanelAsync(WeeklySummaryStudent student, SchoolWeek week, WeeklySummarySource basis, AiDraft? draft) =>
        new(
            student.StudentId,
            student.FullName,
            week.Start,
            week.End,
            week.Label(),
            StatusOf(draft?.Status),
            basis,
            basis.ToBasisLine(await this.BasisTextsAsync().ConfigureAwait(false)),
            !basis.IsEmpty,
            draft is { IsPending: true } ? draft.Id : null,
            draft?.DraftText,
            draft?.FinalText,
            draft?.ApprovedAt);

    private async Task<string> GenerateAsync(WeeklySummarySource source, WeeklySummaryStudent student, SchoolWeek week, CancellationToken cancellationToken)
    {
        string reason;
        try
        {
            AiTextResult result = await this.generator.GenerateAsync(
                new AiTextRequest(AiPrompts.WeeklySummary, source.ToPromptText(), AiPrompts.WeeklySummaryMaxTokens),
                cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(result.Text))
            {
                return result.Text;
            }

            reason = "empty";
        }
        catch (AiUnavailableException exception)
        {
            reason = exception.Reason;
        }

        this.logger.LogWarning("AI kullanılamıyor (haftalık özet): neden {Reason}, öğrenci {StudentId}, hafta {WeekStart}, öğretmen {UserId}", reason, student.StudentId, week.Start, student.TeacherUserId);
        throw new BusinessException(await this.localization.AiTextAsync(AiMessages.SummaryUnavailable).ConfigureAwait(false));
    }

    private async Task<IExclusiveScope> LockAsync(Guid studentId, SchoolWeek week, CancellationToken cancellationToken)
    {
        try
        {
            return await this.unitOfWork.BeginExclusiveAsync(WeeklySummaryLock.Key(studentId, week.Start), cancellationToken).ConfigureAwait(false);
        }
        catch (ExclusiveLockTimeoutException)
        {
            this.logger.LogWarning("Haftalık özet sırası dolu: öğrenci {StudentId}, hafta {WeekStart}", studentId, week.Start);
            throw new BusinessException(await this.localization.AiTextAsync(AiMessages.SummaryBusy).ConfigureAwait(false));
        }
    }

    private async Task<BusinessException> AlreadySentAsync(WeeklySummaryStudent student, SchoolWeek week)
    {
        this.logger.LogWarning("Haftalık özet zaten gönderildi: öğrenci {StudentId}, hafta {WeekStart}, öğretmen {UserId}", student.StudentId, week.Start, student.TeacherUserId);
        return new BusinessException(await this.localization.AiTextAsync(AiMessages.SummaryAlreadySent).ConfigureAwait(false));
    }

    private async Task<WeeklySummaryBasisTexts> BasisTextsAsync() => new(
        await this.localization.AiTextAsync(AiMessages.BasisPrefix).ConfigureAwait(false),
        await this.localization.AiTextAsync(AiMessages.BasisAttendance).ConfigureAwait(false),
        await this.localization.AiTextAsync(AiMessages.BasisLate).ConfigureAwait(false),
        await this.localization.AiTextAsync(AiMessages.BasisNoAttendance).ConfigureAwait(false),
        await this.localization.AiTextAsync(AiMessages.BasisAssignments).ConfigureAwait(false),
        await this.localization.AiTextAsync(AiMessages.BasisGrades).ConfigureAwait(false),
        await this.localization.AiTextAsync(AiMessages.BasisTeacherNote).ConfigureAwait(false));
}
