using System.Net;
using System.Text.Json;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Ai.WeeklySummaryTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Spec 0014 AC-10 (S1, BR-2): veli akışında yalnızca kendi çocuklarının özetlerini görür — farklı şubedeki kardeş dahil; başka velinin
/// çocuğunun özeti akışında yok ve doğrudan istendiğinde (<c>GET api/notices/{id}</c>) 404; veli rolü olmayan 403; ayrıntıda özet, etiket
/// ve dayanak satırı ile başlık; açılış okundu yapar.
/// </summary>
[Trait("Category", "Integration")]
public sealed class WeeklySummaryGuardianFeedTests
{
    private readonly TestDatabase database;

    public WeeklySummaryGuardianFeedTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Feed_OwnChildrenAcrossBranches_OthersHiddenAnd404()
    {
        using OkulApiFactory factory = Factory(this.database, new FakeAiTextGenerator());
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid tenant = w.Class.School.TenantId;
        Guid avcilar = await CreateBranchAsync(client, w.Class.Founder, "Avcılar");
        Guid sixA = await CreateClassroomAsync(client, w.Class.Founder, w.Class.TermId, avcilar, w.Class.LevelId, "6-A");
        (Guid avcilarTeacherId, _, string avcilarTeacher) = await TeacherAsync(factory, client, tenant, avcilar);
        await AssignAsync(client, w.Class.Founder, sixA, avcilarTeacherId);
        Guid can = await CreateStudentAsync(client, w.Class.Founder, NewStudent(avcilar, "Can Yılmaz", classroomId: sixA));
        await LinkAsync(client, w.Class.Founder, can, Guardian(w.AliMother.Email, relationship: "Mother"));

        await AttendAsync(client, w);
        await SavedAsync(client, avcilarTeacher, sixA, Marks([can]));
        await GeneratedAsync(client, w.Class.Teacher, w.Ali);
        await GeneratedAsync(client, avcilarTeacher, can);
        (await SendSummaryAsync(client, w.Class.Teacher, w.Ali, "Ali'nin haftası.")).Status.ShouldBe(HttpStatusCode.OK);
        (await SendSummaryAsync(client, avcilarTeacher, can, "Can'ın haftası.")).Status.ShouldBe(HttpStatusCode.OK);

        JsonElement[] mine = WeeklyItems((await FeedAsync(client, w.AliMother.Token)).Body);
        mine.Select(i => i.GetProperty("childNames")[0].GetString()).ShouldBe(["Ali Yılmaz", "Can Yılmaz"], ignoreOrder: true);
        mine.ShouldAllBe(i => i.GetProperty("title").GetString() == "Haftalık gelişim özeti — " + ThisWeekLabel);
        WeeklyItems((await FeedAsync(client, w.AliFather.Token)).Body).ShouldHaveSingleItem().GetProperty("childNames")[0].GetString().ShouldBe("Ali Yılmaz");
        WeeklyItems((await FeedAsync(client, w.AyseMother.Token)).Body).ShouldBeEmpty("başka velinin çocuğunun özeti görünmez");

        GuardianNotice aliToMother = (await SummaryNoticesAsync(factory, tenant)).Single(n => n.StudentId == w.Ali && n.GuardianUserId == w.AliMother.UserId);
        (await NoticeAsync(client, w.AyseMother.Token, aliToMother.Id)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await NoticeAsync(client, w.AliFather.Token, aliToMother.Id)).Status.ShouldBe(HttpStatusCode.NotFound, "başka velinin bildirimi");
        (await NoticeAsync(client, w.Class.Teacher, aliToMother.Id)).Status.ShouldBe(HttpStatusCode.Forbidden, "veli rolü yok");

        var (status, detail, raw) = await NoticeAsync(client, w.AliMother.Token, aliToMother.Id);
        status.ShouldBe(HttpStatusCode.OK, raw);
        detail.GetProperty("studentName").GetString().ShouldBe("Ali Yılmaz");
        detail.GetProperty("title").GetString().ShouldBe("Haftalık gelişim özeti — " + ThisWeekLabel);
        string body = detail.GetProperty("text").GetString()!;
        body.ShouldStartWith("Ali'nin haftası.");
        body.ShouldContain(Label);
        body.ShouldContain("Dayanak: 1 gün geldi, 0 gün gelmedi");
        WeeklyItems((await FeedAsync(client, w.AliMother.Token)).Body).Single(i => i.GetProperty("id").GetGuid() == aliToMother.Id)
            .GetProperty("isRead").GetBoolean().ShouldBeTrue("ayrıntı açılınca okundu");
    }

    private static JsonElement[] WeeklyItems(JsonElement feed) =>
        [.. feed.GetProperty("items").EnumerateArray().Where(i => i.GetProperty("kind").GetString() == "WeeklySummary")];
}
