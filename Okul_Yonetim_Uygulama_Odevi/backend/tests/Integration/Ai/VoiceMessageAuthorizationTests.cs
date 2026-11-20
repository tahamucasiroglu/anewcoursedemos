using System.Net;
using OkulYonetim.Domain.Ai;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Ai.AiTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Spec 0012 AC-4 (S1, S5): veli (konuşmanın tarafı) 403; konuşmada taraf olmayan öğretmen, kurucu ve muhasebe 404; başka
/// okulun konuşması 404; girişsiz istek 401; ilişkisi biten konuşmada "Bu konuşmaya artık mesaj gönderilemez."; hiçbirinde
/// taslak oluşmaz ve ses servisi çağrılmaz. Taslak başka öğretmen tarafından, başka konuşmada ya da veli tarafından
/// kullanılamaz/silinemez (404) ve bekler durumda kalır.
/// </summary>
[Trait("Category", "Integration")]
public sealed class VoiceMessageAuthorizationTests
{
    private readonly TestDatabase database;

    public VoiceMessageAuthorizationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task VoiceNote_NotTheConversationTeacher_RejectedNoDraft()
    {
        FakeSpeechToText speech = new();
        using OkulApiFactory factory = Factory(this.database, speech, new FakeAiTextGenerator());
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid conversation = await StartedAsync(client, w.Class.Teacher, w.Ali, w.AliMother.UserId);
        string accountantEmail = UniqueEmail("muhasebe");
        await AddMemberAsync(factory, accountantEmail, w.Class.School.TenantId, Role.Accounting, w.Class.School.BranchId);
        string accountant = await TokenForSchoolAsync(client, accountantEmail, w.Class.School.TenantId);

        var (guardianStatus, guardianProblem, _) = await VoiceNoteAsync(client, w.AliMother.Token, conversation);
        guardianStatus.ShouldBe(HttpStatusCode.Forbidden, "veli taraf ama öğretmen değil");
        Detail(guardianProblem).ShouldBe("Sesli notu yalnızca konuşmanın öğretmeni kullanabilir.");
        (await VoiceNoteAsync(client, w.OtherTeacher, conversation)).Status.ShouldBe(HttpStatusCode.NotFound, "taraf olmayan öğretmen");
        (await VoiceNoteAsync(client, w.AliFather.Token, conversation)).Status.ShouldBe(HttpStatusCode.NotFound, "aynı öğrencinin diğer velisi taraf değil");
        (await VoiceNoteAsync(client, w.Class.Founder, conversation)).Status.ShouldBe(HttpStatusCode.NotFound, "kurucu konuşmayı görmez");
        (await VoiceNoteAsync(client, accountant, conversation)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await VoiceNoteAsync(client, null, conversation)).Status.ShouldBe(HttpStatusCode.Unauthorized);

        speech.Calls.ShouldBe(0);
        (await DraftsAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task VoiceNote_OtherSchoolConversation_NotFound()
    {
        FakeSpeechToText speech = new();
        using OkulApiFactory factory = Factory(this.database, speech, new FakeAiTextGenerator());
        using HttpClient client = factory.CreateClient();
        CommunicationWorld a = await WorldAsync(factory, client);
        CommunicationWorld b = await WorldAsync(factory, client);
        Guid inB = await StartedAsync(client, b.Class.Teacher, b.Ali, b.AliMother.UserId);

        var (status, problem, _) = await VoiceNoteAsync(client, a.Class.Teacher, inB);

        status.ShouldBe(HttpStatusCode.NotFound);
        Detail(problem).ShouldBe("Konuşma bulunamadı");
        speech.Calls.ShouldBe(0);
        (await DraftsAsync(factory, a.Class.School.TenantId)).ShouldBeEmpty();
        (await DraftsAsync(factory, b.Class.School.TenantId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task VoiceNote_RelationEnded_CannotSendNoDraft()
    {
        FakeSpeechToText speech = new();
        using OkulApiFactory factory = Factory(this.database, speech, new FakeAiTextGenerator());
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid conversation = await StartedAsync(client, w.Class.Teacher, w.Ali, w.AliMother.UserId);
        await AssignAsync(client, w.Class.Founder, w.Class.ClassroomId);

        var (status, problem, _) = await VoiceNoteAsync(client, w.Class.Teacher, conversation);

        status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Bu konuşmaya artık mesaj gönderilemez.");
        speech.Calls.ShouldBe(0);
        (await DraftsAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Draft_OtherTeacherOtherConversationOrGuardian_NotFoundAndStaysPending()
    {
        using OkulApiFactory factory = Factory(this.database, new FakeSpeechToText(), new FakeAiTextGenerator());
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid withAli = await StartedAsync(client, w.Class.Teacher, w.Ali, w.AliMother.UserId);
        Guid withAyse = await StartedAsync(client, w.Class.Teacher, w.Ayse, w.AyseMother.UserId);
        Guid otherTeachers = await StartedAsync(client, w.OtherTeacher, w.Berk, w.BerkMother.UserId);
        Guid draftId = await DraftedAsync(client, w.Class.Teacher, withAli);

        var (otherStatus, otherProblem, _) = await SendDraftAsync(client, w.OtherTeacher, otherTeachers, "başkasının taslağı", draftId);
        otherStatus.ShouldBe(HttpStatusCode.NotFound);
        Detail(otherProblem).ShouldBe("Taslak bulunamadı");
        (await SendDraftAsync(client, w.Class.Teacher, withAyse, "başka konuşma", draftId)).Status.ShouldBe(HttpStatusCode.NotFound, "taslak oluşturulduğu konuşmada kullanılır");
        (await SendDraftAsync(client, w.AliMother.Token, withAli, "veli", draftId)).Status.ShouldBe(HttpStatusCode.NotFound, "taslak yalnızca oluşturanın");
        (await DiscardAsync(client, w.OtherTeacher, draftId)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await DiscardAsync(client, w.AliMother.Token, draftId)).Status.ShouldBe(HttpStatusCode.NotFound);

        (await MessageCountAsync(factory, otherTeachers)).ShouldBe(0);
        (await MessageCountAsync(factory, withAyse)).ShouldBe(0);
        (await MessageCountAsync(factory, withAli)).ShouldBe(0);
        (await DraftsAsync(factory, w.Class.School.TenantId)).ShouldHaveSingleItem().Status.ShouldBe(AiDraftStatus.Pending);
    }

    [Fact]
    public async Task Draft_OtherSchoolTeacher_CannotDiscard()
    {
        using OkulApiFactory factory = Factory(this.database, new FakeSpeechToText(), new FakeAiTextGenerator());
        using HttpClient client = factory.CreateClient();
        CommunicationWorld a = await WorldAsync(factory, client);
        CommunicationWorld b = await WorldAsync(factory, client);
        Guid conversation = await StartedAsync(client, a.Class.Teacher, a.Ali, a.AliMother.UserId);
        Guid draftId = await DraftedAsync(client, a.Class.Teacher, conversation);

        (await DiscardAsync(client, b.Class.Teacher, draftId)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await DraftsAsync(factory, a.Class.School.TenantId)).ShouldHaveSingleItem();
    }
}
