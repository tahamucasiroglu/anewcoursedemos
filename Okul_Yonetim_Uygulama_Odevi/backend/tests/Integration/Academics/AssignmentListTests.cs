using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Academics.AcademicsTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Academics;

/// <summary>
/// Spec 0013 AC-3: sınıfa atanmış öğretmen (vereni dahil, sınıfın başka öğretmeni de) sınıfın bütün ödevlerini teslim tarihine
/// göre yeniden eskiye, ders, başlık, teslim tarihi, veren öğretmen ve doğru "x / y teslim etti" sayısıyla görür; ödevi olmayan
/// sınıfta liste boştur (ekranda "Henüz ödev yok").
/// </summary>
[Trait("Category", "Integration")]
public sealed class AssignmentListTests
{
    private readonly TestDatabase database;

    public AssignmentListTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task List_NewestDueFirst_WithTeacherAndSubmittedCounts()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid secondId, _, string second) = await TeacherAsync(factory, client, w.Class.School.TenantId, w.Class.School.BranchId);
        await AssignAsync(client, w.Class.Founder, w.Class.ClassroomId, w.Class.TeacherId, secondId);

        (await AssignmentsAsync(client, w.Class.Teacher, w.Class.ClassroomId)).Body.GetArrayLength().ShouldBe(0);

        Guid soon = await AssignedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Homework("Fen", "Deney", null, "2026-11-23"));
        await AssignedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Homework("Türkçe", "Okuma", null, "2026-12-10"));
        await AssignedAsync(client, second, w.Class.ClassroomId, Homework("Matematik", "Kesirler", null, "2026-11-30"));
        (await SaveSubmissionsAsync(client, w.Class.Teacher, soon, (w.Ali, "Submitted"), (w.Ayse, "NotSubmitted"))).Status.ShouldBe(HttpStatusCode.OK);

        foreach (string token in new[] { w.Class.Teacher, second })
        {
            var (status, list, _) = await AssignmentsAsync(client, token, w.Class.ClassroomId);
            status.ShouldBe(HttpStatusCode.OK);
            RowTitles(list).ShouldBe(["Okuma", "Kesirler", "Deney"]);
            JsonElement deney = list[2];
            deney.GetProperty("subject").GetString().ShouldBe("Fen");
            deney.GetProperty("dueDate").GetString().ShouldBe("2026-11-23");
            deney.GetProperty("createdByName").GetString().ShouldBe("Test Kişi");
            deney.GetProperty("submittedCount").GetInt32().ShouldBe(1);
            deney.GetProperty("studentCount").GetInt32().ShouldBe(2);
            list[0].GetProperty("submittedCount").GetInt32().ShouldBe(0);
        }

        (await AssignmentsAsync(client, w.OtherTeacher, w.OtherClassroomId)).Body.GetArrayLength().ShouldBe(0, "başka sınıfta ödev yok");
    }
}
