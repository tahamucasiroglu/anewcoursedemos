using System.Net;
using OkulYonetim.Domain.Ai;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Ai.WeeklySummaryTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Spec 0014 AC-9 (S1, S3; plan 0014 Risks 9 — öğrenci bazlı okuma sorguları erişim denetlemez, guard denetler): sınıfa atanmamış
/// öğretmen ve başka sınıfın öğrencisi 404 (liste, panel, üretim, gönderim, vazgeç); öğretmen rolü olmayan kurucu, veli ve muhasebe
/// 403; başka okulun öğretmeni 404; girişsiz 401; başka sınıfın taslağı onaylanamaz ve silinemez (404); hiçbir reddedilen istekte
/// taslak ya da bildirim oluşmaz ve model çağrılmaz. Review R1-1: haftalık özet taslağı 0012'nin Vazgeç ucundan
/// (<c>DELETE api/ai/drafts/{id}</c>) oluşturanınca da silinemez — sınıftan çıkarılmış olsa bile (404, taslak durur).
/// </summary>
[Trait("Category", "Integration")]
public sealed class WeeklySummaryAuthorizationTests
{
    private readonly TestDatabase database;

    public WeeklySummaryAuthorizationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task UnassignedTeacherOtherClassOtherSchool_404_NothingWritten()
    {
        FakeAiTextGenerator text = new();
        using OkulApiFactory factory = Factory(this.database, text);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await AttendAsync(client, w);
        SchoolSeed otherSchool = await CreateSchoolAsync(factory);
        string otherSchoolTeacherEmail = UniqueEmail("ogretmen");
        await AddMemberAsync(factory, otherSchoolTeacherEmail, otherSchool.TenantId, Role.Teacher, otherSchool.BranchId);
        string otherSchoolTeacher = await TokenForSchoolAsync(client, otherSchoolTeacherEmail, otherSchool.TenantId);

        // 5-B'nin öğretmeni 5-A'nın öğrencisine, 5-A'nın öğretmeni 5-B'nin öğrencisine (Berk), başka okulun öğretmeni Ali'ye.
        foreach ((string token, Guid student) in new[] { (w.OtherTeacher, w.Ali), (w.Class.Teacher, w.Berk), (otherSchoolTeacher, w.Ali) })
        {
            (await PanelAsync(client, token, student)).Status.ShouldBe(HttpStatusCode.NotFound);
            (await GenerateAsync(client, token, student, "Not.")).Status.ShouldBe(HttpStatusCode.NotFound);
            (await SendSummaryAsync(client, token, student, "Metin.")).Status.ShouldBe(HttpStatusCode.NotFound);
            (await DiscardSummaryAsync(client, token, student)).Status.ShouldBe(HttpStatusCode.NotFound);
        }

        (await ListAsync(client, w.OtherTeacher, w.Class.ClassroomId)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await ListAsync(client, otherSchoolTeacher, w.Class.ClassroomId)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await ListAsync(client, w.Class.Teacher, Guid.NewGuid())).Status.ShouldBe(HttpStatusCode.NotFound);
        (await PanelAsync(client, w.Class.Teacher, Guid.NewGuid())).Status.ShouldBe(HttpStatusCode.NotFound);
        text.Requests.ShouldBeEmpty("reddedilen istekte model çağrılmaz");
        (await SummaryDraftsAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty();
        (await SummaryNoticesAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task OtherClassDraft_CannotBeApprovedOrDiscarded()
    {
        FakeAiTextGenerator text = new();
        using OkulApiFactory factory = Factory(this.database, text);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await SavedAsync(client, w.OtherTeacher, w.OtherClassroomId, Marks([w.Berk]));
        await GeneratedAsync(client, w.OtherTeacher, w.Berk);

        (await SendSummaryAsync(client, w.Class.Teacher, w.Berk, "Başka sınıfın özeti.")).Status.ShouldBe(HttpStatusCode.NotFound);
        (await DiscardSummaryAsync(client, w.Class.Teacher, w.Berk)).Status.ShouldBe(HttpStatusCode.NotFound);

        (await SummaryDraftsAsync(factory, w.Class.School.TenantId)).ShouldHaveSingleItem().IsPending.ShouldBeTrue();
        (await SummaryNoticesAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task NonTeacherRoles_403_Anonymous401()
    {
        FakeAiTextGenerator text = new();
        using OkulApiFactory factory = Factory(this.database, text);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await AttendAsync(client, w);
        string accountantEmail = UniqueEmail("muhasebe");
        await AddMemberAsync(factory, accountantEmail, w.Class.School.TenantId, Role.Accounting, w.Class.School.BranchId);
        string accountant = await TokenForSchoolAsync(client, accountantEmail, w.Class.School.TenantId);

        foreach (string token in new[] { w.Class.Founder, w.AliMother.Token, accountant })
        {
            (await ClassroomsAsync(client, token)).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await ListAsync(client, token, w.Class.ClassroomId)).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await PanelAsync(client, token, w.Ali)).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await GenerateAsync(client, token, w.Ali, "Not.")).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await SendSummaryAsync(client, token, w.Ali, "Metin.")).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await DiscardSummaryAsync(client, token, w.Ali)).Status.ShouldBe(HttpStatusCode.Forbidden);
        }

        foreach ((HttpMethod method, string path) in new[]
        {
            (HttpMethod.Get, "/api/weekly-summaries/classrooms"),
            (HttpMethod.Get, $"/api/weekly-summaries?classroomId={w.Class.ClassroomId}"),
            (HttpMethod.Get, $"/api/weekly-summaries/students/{w.Ali}"),
            (HttpMethod.Post, $"/api/weekly-summaries/students/{w.Ali}/draft"),
            (HttpMethod.Post, $"/api/weekly-summaries/students/{w.Ali}/send"),
            (HttpMethod.Delete, $"/api/weekly-summaries/students/{w.Ali}/draft"),
            (HttpMethod.Get, $"/api/notices/{Guid.NewGuid()}"),
        })
        {
            using HttpRequestMessage request = new(method, new Uri(path, UriKind.Relative));
            if (method == HttpMethod.Post)
            {
                request.Content = System.Net.Http.Json.JsonContent.Create(new { text = "x", teacherNote = "x" });
            }

            using HttpResponseMessage anonymous = await client.SendAsync(request, TestContext.Current.CancellationToken);
            anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, path);
        }

        text.Requests.ShouldBeEmpty();
        (await SummaryDraftsAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty();
        (await SummaryNoticesAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task UnassignedAfterDraft_NextRequestRejected()
    {
        using OkulApiFactory factory = Factory(this.database, new FakeAiTextGenerator());
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid secondId, _, string second) = await TeacherAsync(factory, client, w.Class.School.TenantId, w.Class.School.BranchId);
        await AssignAsync(client, w.Class.Founder, w.Class.ClassroomId, w.Class.TeacherId, secondId);
        await AttendAsync(client, w);
        await GeneratedAsync(client, w.Class.Teacher, w.Ali);

        await AssignAsync(client, w.Class.Founder, w.Class.ClassroomId, secondId);

        (await SendSummaryAsync(client, w.Class.Teacher, w.Ali, "Atamadan çıkarılan öğretmen.")).Status.ShouldBe(HttpStatusCode.NotFound);
        (await SummaryNoticesAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty();
        (await SendSummaryAsync(client, second, w.Ali, "Sınıfın öğretmeni gönderir.")).Status.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WeeklySummaryDraft_VoiceDraftDiscardEndpoint_404_DraftRemains()
    {
        using OkulApiFactory factory = Factory(this.database, new FakeAiTextGenerator());
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid secondId, _, _) = await TeacherAsync(factory, client, w.Class.School.TenantId, w.Class.School.BranchId);
        await AttendAsync(client, w);
        Guid draftId = (await GeneratedAsync(client, w.Class.Teacher, w.Ali)).GetProperty("draftId").GetGuid();

        (await AiTestData.DiscardAsync(client, w.Class.Teacher, draftId)).Status.ShouldBe(HttpStatusCode.NotFound, "özet taslağı yalnızca kendi ucundan (guard + sıra) silinir");
        await AssignAsync(client, w.Class.Founder, w.Class.ClassroomId, secondId);
        var (status, problem, raw) = await AiTestData.DiscardAsync(client, w.Class.Teacher, draftId);
        status.ShouldBe(HttpStatusCode.NotFound, raw);
        Detail(problem).ShouldBe("Taslak bulunamadı", "bulunamayanla aynı mesaj");

        AiDraft draft = (await SummaryDraftsAsync(factory, w.Class.School.TenantId)).ShouldHaveSingleItem();
        draft.Id.ShouldBe(draftId);
        draft.IsPending.ShouldBeTrue();
    }
}
