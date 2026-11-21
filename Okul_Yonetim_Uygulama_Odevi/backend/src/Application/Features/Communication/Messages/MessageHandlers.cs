using System.Globalization;
using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Ai.Drafts;
using OkulYonetim.Application.Features.Communication.Notifications;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Features.Students.Queries.ListClassroomRoster;
using OkulYonetim.Application.Features.Students.Queries.ListGuardianContacts;
using OkulYonetim.Application.Features.Students.Queries.ListStudentBriefs;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Ai;
using OkulYonetim.Domain.Communication;

namespace OkulYonetim.Application.Features.Communication.Messages;

/// <summary>
/// Öğretmen–veli birebir mesajlaşma ve sessiz mod (spec 0011 gereksinim 5, 6). Konuşma bir öğrenci bağlamındadır ve
/// öğretmen–veli–öğrenci üçlüsü için tektir; açmak ve her mesaj <see cref="CommunicationGuard.IsRelatedAsync"/> ilişkisini
/// ister (ilişki bitince eski konuşma okunur, yeni mesaj "Bu konuşmaya artık mesaj gönderilemez."). Konuşmayı yalnızca iki
/// taraf görür (kurucu dahil başkası 404); öğretmen ya da veli rolü olmayan kişi konuşma başlatamaz (403).
/// <para>
/// Sessiz mod: veli mesajı öğretmenin mesai saatleri tanımlıysa ve şu an (Türkiye saati) seçili günde [başlangıç, bitiş)
/// içinde değilse bildirim üretmez — mesaj yine iletilir, okunmamış sayısı artar, veliye "mesai dışında" bilgisi döner.
/// Öğretmenden veliye mesaj her zaman bildirim üretir. Bildirim kayıttan sonra bildirim kapısından çıkar. Mesaj metni audit'e
/// ve loga yazılmaz; Information logunda yalnızca kimlikler.
/// </para>
/// <para>
/// Spec 0012 (AI taslağıyla gönderim, BR-5): taslak kimliği verilirse taslak sırası (<c>ai-draft:&lt;taslak&gt;</c>) içinde Ai'ın
/// onay kapısı çağrılır — taslak gönderenin, sesli not türünde ve bu konuşmada olmalı (yoksa 404), kullanılmışsa "Bu taslak
/// zaten kullanıldı."; mesaj öğretmenin son metniyle yazılır ve taslak aynı kayıtta son metin + mesaj kimliğiyle onaylanır.
/// Aynı taslakla eşzamanlı ikinci gönderim sırada bekler ve reddedilir.
/// </para>
/// </summary>
public sealed class MessageHandlers :
    IRequestHandler<StartConversationCommand, ConversationRow>,
    IRequestHandler<ListConversationsQuery, IReadOnlyList<ConversationRow>>,
    IRequestHandler<GetConversationMessagesQuery, ConversationThread>,
    IRequestHandler<SendMessageCommand, MessageSent>,
    IRequestHandler<ListMessageContactsQuery, MessageContacts>,
    IRequestHandler<GetTeacherConversationContextQuery, TeacherConversationContext>
{
    public const int PageSize = 50;
    public const int MaxListed = 100;

    private static readonly StringComparer TurkishOrder = StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true);

    private readonly CommunicationGuard guard;
    private readonly IConversationRepository conversations;
    private readonly IQuietHoursRepository quietHours;
    private readonly ISender sender;
    private readonly SchoolCalendar calendar;
    private readonly IUnitOfWork unitOfWork;
    private readonly GuardianNotifier notifier;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<MessageHandlers> logger;

    public MessageHandlers(
        CommunicationGuard guard,
        IConversationRepository conversations,
        IQuietHoursRepository quietHours,
        ISender sender,
        SchoolCalendar calendar,
        IUnitOfWork unitOfWork,
        GuardianNotifier notifier,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<MessageHandlers> logger)
    {
        this.guard = guard;
        this.conversations = conversations;
        this.quietHours = quietHours;
        this.sender = sender;
        this.calendar = calendar;
        this.unitOfWork = unitOfWork;
        this.notifier = notifier;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<ConversationRow> Handle(StartConversationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        CommunicationCaller caller = await this.guard.CallerAsync(cancellationToken).ConfigureAwait(false);
        if (!caller.IsTeacher && !caller.IsGuardian)
        {
            throw await this.guard.ForbiddenAsync().ConfigureAwait(false);
        }

        Guid studentId = request.StudentId!.Value;
        Guid other = request.CounterpartUserId!.Value;
        (Guid Teacher, Guid Guardian)? parties = null;
        if (caller.IsGuardian && await this.guard.IsRelatedAsync(other, caller.UserId, studentId, cancellationToken).ConfigureAwait(false))
        {
            parties = (other, caller.UserId);
        }
        else if (caller.IsTeacher && await this.guard.IsRelatedAsync(caller.UserId, other, studentId, cancellationToken).ConfigureAwait(false))
        {
            parties = (caller.UserId, other);
        }

        if (parties is not { } pair)
        {
            throw await this.guard.NotFoundAsync(CommunicationMessages.ContactNotFound).ConfigureAwait(false);
        }

        IExclusiveScope scope;
        try
        {
            scope = await this.unitOfWork.BeginExclusiveAsync(
                "conversation:" + pair.Teacher.ToString("N") + ":" + pair.Guardian.ToString("N") + ":" + studentId.ToString("N"),
                cancellationToken).ConfigureAwait(false);
        }
        catch (ExclusiveLockTimeoutException)
        {
            throw new BusinessException(await this.localization.CommunicationTextAsync(CommunicationMessages.TooManyRequests).ConfigureAwait(false));
        }

        Conversation? conversation;
        await using (scope.ConfigureAwait(false))
        {
            conversation = await this.conversations.FindByPartiesAsync(pair.Teacher, pair.Guardian, studentId, cancellationToken).ConfigureAwait(false);
            if (conversation is null)
            {
                conversation = Conversation.Start(caller.TenantId, pair.Teacher, pair.Guardian, studentId, this.timeProvider.GetUtcNow());
                this.conversations.Add(conversation);
                await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                this.logger.LogInformation(
                    "Konuşma açıldı: konuşma {ConversationId}, öğretmen {TeacherId}, veli {GuardianId}, öğrenci {StudentId}",
                    conversation.Id,
                    pair.Teacher,
                    pair.Guardian,
                    studentId);
            }

            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return (await this.conversations.ListForUserAsync(caller.UserId, conversation.Id, 1, cancellationToken).ConfigureAwait(false)).Single();
    }

    public async Task<IReadOnlyList<ConversationRow>> Handle(ListConversationsQuery request, CancellationToken cancellationToken)
    {
        CommunicationCaller caller = await this.guard.CallerAsync(cancellationToken).ConfigureAwait(false);
        return await this.conversations.ListForUserAsync(caller.UserId, null, MaxListed, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ConversationThread> Handle(GetConversationMessagesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        CommunicationCaller caller = await this.guard.CallerAsync(cancellationToken).ConfigureAwait(false);
        Conversation conversation = await this.PartyConversationAsync(request.Id, caller.UserId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<ConversationMessage> page = await this.conversations.ListMessagesAsync(conversation.Id, request.Before, PageSize + 1, cancellationToken).ConfigureAwait(false);
        // Okundu = dönen en yeni mesajın zamanı ve yalnızca ilk sayfada (review L-2): okuma ile kayıt arasında gelen mesaj ve
        // "Daha eski" sayfası okunmamış sayısını sıfırlamaz.
        if (request.Before is null && page.Count > 0)
        {
            conversation.MarkReadBy(caller.UserId, page[0].SentAt);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        bool canSend = await this.guard.IsRelatedAsync(conversation.TeacherUserId, conversation.GuardianUserId, conversation.StudentId, cancellationToken).ConfigureAwait(false);
        ConversationRow row = (await this.conversations.ListForUserAsync(caller.UserId, conversation.Id, 1, cancellationToken).ConfigureAwait(false)).Single();
        return new ConversationThread(
            row,
            canSend,
            [.. page.Take(PageSize).Reverse().Select(m => new MessageView(m.Id, m.SenderUserId == caller.UserId, m.Body, m.SentAt))],
            page.Count > PageSize);
    }

    public async Task<MessageSent> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.AiDraftId is not { } draftId)
        {
            return await this.SendAsync(request, null, cancellationToken).ConfigureAwait(false);
        }

        IExclusiveScope scope;
        try
        {
            scope = await this.unitOfWork.BeginExclusiveAsync(AiDraftLock.Key(draftId), cancellationToken).ConfigureAwait(false);
        }
        catch (ExclusiveLockTimeoutException)
        {
            throw new BusinessException(await this.localization.CommunicationTextAsync(CommunicationMessages.TooManyRequests).ConfigureAwait(false));
        }

        await using (scope.ConfigureAwait(false))
        {
            return await this.SendAsync(request, scope, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Gönderimin kendisi; AI taslağıyla gönderimde taslak sırası içinde çağrılır ve kayıt sonrası sıra commit edilir (bildirim commit'ten sonra).</summary>
    private async Task<MessageSent> SendAsync(SendMessageCommand request, IExclusiveScope? draftScope, CancellationToken cancellationToken)
    {
        CommunicationCaller caller = await this.guard.CallerAsync(cancellationToken).ConfigureAwait(false);
        Conversation conversation = await this.PartyConversationAsync(request.ConversationId, caller.UserId, cancellationToken).ConfigureAwait(false);
        if (!await this.guard.IsRelatedAsync(conversation.TeacherUserId, conversation.GuardianUserId, conversation.StudentId, cancellationToken).ConfigureAwait(false))
        {
            throw new BusinessException(await this.localization.CommunicationTextAsync(CommunicationMessages.CannotSend).ConfigureAwait(false));
        }

        if (request.AiDraftId is { } draftId)
        {
            await this.sender.Send(new GetPendingAiDraftQuery(draftId, AiDraftKind.VoiceMessage, conversation.Id), cancellationToken).ConfigureAwait(false);
        }

        DateTimeOffset now = this.timeProvider.GetUtcNow();
        ConversationMessage message = ConversationMessage.Write(conversation, caller.UserId, request.Body!, now);
        this.conversations.AddMessage(message);
        conversation.Record(caller.UserId, now);
        if (request.AiDraftId is { } approvedDraftId)
        {
            await this.sender.Send(new MarkAiDraftApprovedCommand(approvedDraftId, message.Body, message.Id), cancellationToken).ConfigureAwait(false);
        }

        bool fromGuardian = caller.UserId == conversation.GuardianUserId;
        Guid recipient = fromGuardian ? conversation.TeacherUserId : conversation.GuardianUserId;
        bool outsideWorkingHours = false;
        if (fromGuardian && await this.quietHours.FindAsync(conversation.TeacherUserId, cancellationToken).ConfigureAwait(false) is { } hours)
        {
            DateTimeOffset local = this.calendar.TurkeyTime(now);
            outsideWorkingHours = !hours.IsWorkingTime(local.DayOfWeek, TimeOnly.FromDateTime(local.DateTime));
        }

        await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (draftScope is not null)
        {
            await draftScope.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        this.logger.LogInformation(
            "Mesaj gönderildi: konuşma {ConversationId}, mesaj {MessageId}, gönderen {UserId}, bildirim {Notify}, AI taslağı {AiDraftId}",
            conversation.Id,
            message.Id,
            caller.UserId,
            !outsideWorkingHours,
            request.AiDraftId);
        if (!outsideWorkingHours)
        {
            await this.notifier.DeliverAsync([new PushNotification(conversation.TenantId, recipient, PushNotificationKind.Message, message.Id)], [], cancellationToken).ConfigureAwait(false);
        }

        return new MessageSent(new MessageView(message.Id, true, message.Body, message.SentAt), outsideWorkingHours);
    }

    public async Task<MessageContacts> Handle(ListMessageContactsQuery request, CancellationToken cancellationToken)
    {
        CommunicationCaller caller = await this.guard.CallerAsync(cancellationToken).ConfigureAwait(false);
        if (!caller.IsTeacher && !caller.IsGuardian)
        {
            throw await this.guard.ForbiddenAsync().ConfigureAwait(false);
        }

        List<ChildTeachers> asGuardian = [];
        IReadOnlyList<ChildRow> children = await this.guard.ChildrenAsync(caller, cancellationToken).ConfigureAwait(false);
        if (children.Count > 0)
        {
            IReadOnlyList<StudentBrief> briefs = await this.sender.Send(new ListStudentBriefsQuery(Ids: [.. children.Select(c => c.Id)]), cancellationToken).ConfigureAwait(false);
            foreach (StudentBrief child in briefs.OrderBy(b => b.FullName, TurkishOrder).ThenBy(b => b.Id))
            {
                IReadOnlyList<ContactPerson> teachers = child.ClassroomId is { } classroomId
                    ? await this.guard.ClassroomTeachersAsync(classroomId, child.BranchId, cancellationToken).ConfigureAwait(false)
                    : [];
                asGuardian.Add(new ChildTeachers(child.Id, child.FullName, [.. teachers.Where(t => t.UserId != caller.UserId).OrderBy(t => t.FullName, TurkishOrder)]));
            }
        }

        List<ClassroomContacts> asTeacher = [];
        foreach (TeacherClassroomRow classroom in (await this.guard.TeacherClassroomsAsync(caller, cancellationToken).ConfigureAwait(false))
            .OrderBy(c => c.Name, TurkishOrder).ThenBy(c => c.ClassroomId))
        {
            IReadOnlyList<RosterStudent> roster = await this.sender.Send(new ListClassroomRosterQuery(classroom.ClassroomId), cancellationToken).ConfigureAwait(false);
            ILookup<Guid, GuardianContact> guardians = roster.Count == 0
                ? Array.Empty<GuardianContact>().ToLookup(g => g.StudentId)
                : (await this.sender.Send(new ListGuardianContactsQuery([.. roster.Select(s => s.Id)]), cancellationToken).ConfigureAwait(false)).ToLookup(g => g.StudentId);
            asTeacher.Add(new ClassroomContacts(
                classroom.ClassroomId,
                classroom.Name,
                [.. roster.Select(s => new StudentContacts(
                    s.Id,
                    s.FullName,
                    [.. guardians[s.Id].Where(g => g.UserId != caller.UserId).OrderBy(g => g.FullName, TurkishOrder).Select(g => new ContactPerson(g.UserId, g.FullName))]))]));
        }

        return new MessageContacts(asGuardian, asTeacher);
    }

    public async Task<TeacherConversationContext> Handle(GetTeacherConversationContextQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        CommunicationCaller caller = await this.guard.CallerAsync(cancellationToken).ConfigureAwait(false);
        Conversation conversation = await this.PartyConversationAsync(request.ConversationId, caller.UserId, cancellationToken).ConfigureAwait(false);
        bool isTeacher = conversation.TeacherUserId == caller.UserId;
        bool canSend = isTeacher
            && await this.guard.IsRelatedAsync(conversation.TeacherUserId, conversation.GuardianUserId, conversation.StudentId, cancellationToken).ConfigureAwait(false);
        return new TeacherConversationContext(conversation.Id, conversation.StudentId, isTeacher, canSend);
    }

    /// <summary>Kişinin taraf olduğu konuşma (izlenen); yoksa ya da taraf değilse 404.</summary>
    private async Task<Conversation> PartyConversationAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        Conversation? conversation = await this.conversations.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return conversation is not null && conversation.HasParty(userId)
            ? conversation
            : throw await this.guard.NotFoundAsync(CommunicationMessages.ConversationNotFound).ConfigureAwait(false);
    }
}
