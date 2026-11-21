using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Academics.AcademicsTestData;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Academics;

/// <summary>
/// Spec 0013 AC-8: veli her çocuğu için bekleyen ödevleri (teslim tarihi bugün ya da sonra, en yakın üstte), teslim tarihi son
/// 30 günde geçmiş ödevleri durumlarıyla ve etkin dönemdeki notları (yeniden eskiye; puan ya da girilmedi) görür; 31 gün önceki
/// ödev görünmez; farklı şubedeki kardeşin özeti de açılır; boş bölümler boş liste döner (ekranda "Bekleyen ödev yok", "Son 30
/// günde ödev yok", "Henüz not yok").
/// </summary>
[Trait("Category", "Integration")]
public sealed class GuardianAcademicsSummaryTests
{
    private readonly TestDatabase database;

    public GuardianAcademicsSummaryTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Summary_PendingNearestFirst_GradesNewestFirst_EmptyChildEmpty()
    {
        using OkulApiFactory factory = CommunicationTestData.Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await AssignedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Homework("Matematik", "Kesirler alıştırması", null, "2026-11-25"));
        await AssignedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Homework("Fen", "Bugün teslim", null, Today));
        await AssignedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Homework("Türkçe", "Yarın teslim", null, Tomorrow));
        Guid exam = await OpenedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Exam("Türkçe", "Written", "1. yazılı", Today));
        await OpenedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Exam("Matematik", "Oral", "Sözlü", "2026-11-10"));
        (await SaveGradesAsync(client, w.Class.Teacher, exam, (w.Ali, 85))).Status.ShouldBe(HttpStatusCode.OK);

        var (status, summary, raw) = await SummaryAsync(client, w.AliMother.Token, w.Ali);

        status.ShouldBe(HttpStatusCode.OK, raw);
        summary.GetProperty("fullName").GetString().ShouldBe("Ali Yılmaz");
        summary.GetProperty("today").GetString().ShouldBe(Today);
        RowTitles(summary.GetProperty("pending")).ShouldBe(["Bugün teslim", "Yarın teslim", "Kesirler alıştırması"]);
        JsonElement first = summary.GetProperty("pending")[2];
        first.GetProperty("subject").GetString().ShouldBe("Matematik");
        first.GetProperty("dueDate").GetString().ShouldBe("2026-11-25");
        first.GetProperty("status").GetString().ShouldBe("Unmarked");
        summary.GetProperty("recent").GetArrayLength().ShouldBe(0);
        JsonElement grades = summary.GetProperty("grades");
        RowTitles(grades).ShouldBe(["1. yazılı", "Sözlü"]);
        grades[0].GetProperty("score").GetInt32().ShouldBe(85);
        grades[0].GetProperty("kind").GetString().ShouldBe("Written");
        grades[1].GetProperty("score").ValueKind.ShouldBe(JsonValueKind.Null, "girilmedi");

        JsonElement empty = (await SummaryAsync(client, w.BerkMother.Token, w.Berk)).Body;
        empty.GetProperty("pending").GetArrayLength().ShouldBe(0);
        empty.GetProperty("recent").GetArrayLength().ShouldBe(0);
        empty.GetProperty("grades").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Summary_After31Days_RecentWithin30WithStatus_OlderHidden()
    {
        FakeTimeProvider time = new(Morning);
        using OkulApiFactory factory = CommunicationTestData.Factory(this.database, time: time);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await AssignedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Homework("Fen", "Otuz bir gün önce", null, Today));
        Guid recent = await AssignedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Homework("Türkçe", "Otuz gün önce", null, Tomorrow));
        Guid middle = await AssignedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Homework("Matematik", "Yirmi gün önce", null, "2026-12-01"));
        (await SaveSubmissionsAsync(client, w.Class.Teacher, recent, (w.Ali, "NotSubmitted"))).Status.ShouldBe(HttpStatusCode.OK);
        (await SaveSubmissionsAsync(client, w.Class.Teacher, middle, (w.Ali, "Submitted"))).Status.ShouldBe(HttpStatusCode.OK);

        time.Advance(TimeSpan.FromDays(31));
        string mother = await ReloginAsync(client, w.Class, w.AliMother);
        string teacher = await TokenForSchoolAsync(client, w.Class.TeacherEmail, w.Class.School.TenantId);
        await AssignedAsync(client, teacher, w.Class.ClassroomId, Homework("Fen", "Yeni ödev", null, "2026-12-22"));

        JsonElement summary = (await SummaryAsync(client, mother, w.Ali)).Body;

        summary.GetProperty("today").GetString().ShouldBe("2026-12-21");
        RowTitles(summary.GetProperty("pending")).ShouldBe(["Yeni ödev"]);
        RowTitles(summary.GetProperty("recent")).ShouldBe(["Yirmi gün önce", "Otuz gün önce"], "yeniden eskiye; 31 gün önceki yok");
        summary.GetProperty("recent")[0].GetProperty("status").GetString().ShouldBe("Submitted");
        summary.GetProperty("recent")[1].GetProperty("status").GetString().ShouldBe("NotSubmitted");
    }

    [Fact]
    public async Task Summary_SiblingInOtherBranch_Opens()
    {
        using OkulApiFactory factory = CommunicationTestData.Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid avcilar = await CreateBranchAsync(client, w.Class.Founder, "Avcılar");
        Guid classroom = await CreateClassroomAsync(client, w.Class.Founder, w.Class.TermId, avcilar, w.Class.LevelId, "6-A");
        (Guid avcilarTeacherId, _, string avcilarTeacher) = await TeacherAsync(factory, client, w.Class.School.TenantId, avcilar);
        await AssignAsync(client, w.Class.Founder, classroom, avcilarTeacherId);
        Guid deniz = await CreateStudentAsync(client, w.Class.Founder, NewStudent(avcilar, "Deniz Yılmaz", classroomId: classroom));
        await LinkAsync(client, w.Class.Founder, deniz, Guardian(w.AliMother.Email));
        await AssignedAsync(client, avcilarTeacher, classroom, Homework("Müzik", "Nota çalışması", null, "2026-11-24"));
        string mother = await ReloginAsync(client, w.Class, w.AliMother);

        var (status, summary, raw) = await SummaryAsync(client, mother, deniz);

        status.ShouldBe(HttpStatusCode.OK, raw);
        RowTitles(summary.GetProperty("pending")).ShouldBe(["Nota çalışması"]);
        RowTitles((await SummaryAsync(client, mother, w.Ali)).Body.GetProperty("pending")).ShouldBeEmpty();
    }
}
