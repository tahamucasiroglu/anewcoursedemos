using System.Net;
using OkulYonetim.Domain.Ai;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Ai.WeeklySummaryTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Spec 0014 AC-3 (BR-5) ve AC-4: "Özet oluştur" dayanak ve taslakla "bekliyor" kaydı oluşturur, veliye bildirim gitmez ve bildirim
/// kapısı çağrılmaz; 800 karakteri aşan model metni 800'e kısaltılır; yeniden oluşturma aynı öğrenci-hafta için tek bekleyen taslak
/// bırakır (aynı kayıt, yeni metin). Yoklama, ödev/not ve öğretmen notu olmayan öğrenci-haftada "Bu hafta için özetlenecek veri yok.",
/// model çağrılmaz, taslak oluşmaz; yalnızca öğretmen notu varsa taslak üretilir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class WeeklySummaryDraftTests
{
    private readonly TestDatabase database;

    public WeeklySummaryDraftTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Generate_WithAttendance_PendingDraftNothingToGuardians()
    {
        FakeAiTextGenerator text = new() { Reply = "Ali bu hafta okula düzenli geldi ve derslere katıldı." };
        CapturingPushNotifier notifier = new();
        using OkulApiFactory factory = Factory(this.database, text, notifier);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await AttendAsync(client, w, (w.Ali, "Late"));
        int pushesBefore = notifier.Sent.Count;

        var (status, panel, raw) = await GenerateAsync(client, w.Class.Teacher, w.Ali);

        status.ShouldBe(HttpStatusCode.OK, raw);
        panel.GetProperty("status").GetString().ShouldBe("Draft");
        panel.GetProperty("draftText").GetString().ShouldBe(text.Reply);
        panel.GetProperty("draftId").GetGuid().ShouldNotBe(Guid.Empty);
        AiDraft draft = (await SummaryDraftsAsync(factory, w.Class.School.TenantId)).ShouldHaveSingleItem();
        draft.Status.ShouldBe(AiDraftStatus.Pending);
        draft.StudentId.ShouldBe(w.Ali);
        draft.PeriodStart.ShouldBe(new DateOnly(2026, 11, 16));
        draft.CreatedByUserId.ShouldBe(w.Class.TeacherId);
        draft.DraftText.ShouldBe(text.Reply);
        draft.SourceText.ShouldContain("\"late\":1", customMessage: "dayanak taslakla saklanır (BR-6)");
        notifier.Sent.Count.ShouldBe(pushesBefore, "bildirim kapısı çağrılmaz");
        (await SummaryNoticesAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty();
        Titles((await FeedAsync(client, w.AliMother.Token)).Body).ShouldNotContain(t => t.Contains("Haftalık", StringComparison.Ordinal));
        (await ListAsync(client, w.Class.Teacher, w.Class.ClassroomId)).Body.GetProperty("students")[0].GetProperty("status").GetString().ShouldBe("Draft");
    }

    [Fact]
    public async Task Generate_ModelTextOver800_ShortenedTo800()
    {
        FakeAiTextGenerator text = new() { Reply = new string('a', 1200) };
        using OkulApiFactory factory = Factory(this.database, text);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await AttendAsync(client, w);

        var (status, panel, raw) = await GenerateAsync(client, w.Class.Teacher, w.Ali);

        status.ShouldBe(HttpStatusCode.OK, raw);
        panel.GetProperty("draftText").GetString()!.Length.ShouldBe(800);
        (await SummaryDraftsAsync(factory, w.Class.School.TenantId)).ShouldHaveSingleItem().DraftText.Length.ShouldBe(800);
    }

    [Fact]
    public async Task Regenerate_SameStudentWeek_SinglePendingDraftReplaced()
    {
        FakeAiTextGenerator text = new() { Reply = "İlk taslak." };
        using OkulApiFactory factory = Factory(this.database, text);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid secondId, _, string second) = await Attendance.AttendanceTestData.TeacherAsync(factory, client, w.Class.School.TenantId, w.Class.School.BranchId);
        await Attendance.AttendanceTestData.AssignAsync(client, w.Class.Founder, w.Class.ClassroomId, w.Class.TeacherId, secondId);
        await AttendAsync(client, w);
        Guid first = (await GeneratedAsync(client, w.Class.Teacher, w.Ali)).GetProperty("draftId").GetGuid();

        text.Reply = "Yeniden oluşturulan taslak.";
        var (status, panel, raw) = await GenerateAsync(client, second, w.Ali, "Kitap okumayı sevdi.");

        status.ShouldBe(HttpStatusCode.OK, raw);
        panel.GetProperty("draftId").GetGuid().ShouldBe(first, "aynı kayıt");
        AiDraft draft = (await SummaryDraftsAsync(factory, w.Class.School.TenantId)).ShouldHaveSingleItem();
        draft.Id.ShouldBe(first);
        draft.IsPending.ShouldBeTrue();
        draft.DraftText.ShouldBe("Yeniden oluşturulan taslak.");
        draft.CreatedByUserId.ShouldBe(secondId, "sınıfın atanmış her öğretmeni aynı taslağı görür ve yeniden oluşturabilir");
        draft.SourceText.ShouldContain("Kitap okumayı sevdi.");
        (await PanelAsync(client, w.Class.Teacher, w.Ali)).Body.GetProperty("draftText").GetString().ShouldBe("Yeniden oluşturulan taslak.");
    }

    [Fact]
    public async Task Generate_NoData_RejectedModelNotCalled_OnlyNote_Drafted()
    {
        FakeAiTextGenerator text = new();
        using OkulApiFactory factory = Factory(this.database, text);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        var (panelStatus, panel, _) = await PanelAsync(client, w.Class.Teacher, w.Ali);
        panelStatus.ShouldBe(HttpStatusCode.OK);
        panel.GetProperty("hasData").GetBoolean().ShouldBeFalse();
        panel.GetProperty("basisLine").GetString().ShouldBe("Dayanak: yoklama yok");

        var (status, problem, raw) = await GenerateAsync(client, w.Class.Teacher, w.Ali);
        status.ShouldBe(HttpStatusCode.BadRequest, raw);
        Detail(problem).ShouldBe(NoData);
        (await GenerateAsync(client, w.Class.Teacher, w.Ali, "   ")).Status.ShouldBe(HttpStatusCode.BadRequest, "yalnızca boşluk not sayılmaz");
        text.Requests.ShouldBeEmpty("model çağrılmaz");
        (await SummaryDraftsAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty();

        var (noteStatus, noted, noteRaw) = await GenerateAsync(client, w.Class.Teacher, w.Ali, "Resim yarışmasında ikinci oldu.");
        noteStatus.ShouldBe(HttpStatusCode.OK, noteRaw);
        noted.GetProperty("status").GetString().ShouldBe("Draft");
        noted.GetProperty("basisLine").GetString().ShouldBe("Dayanak: yoklama yok; öğretmen notu");
        text.Requests.ShouldHaveSingleItem().Content.ShouldContain("Öğretmen notu: Resim yarışmasında ikinci oldu.");
    }
}
