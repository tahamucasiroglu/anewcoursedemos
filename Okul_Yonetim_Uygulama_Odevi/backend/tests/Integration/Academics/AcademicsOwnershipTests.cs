using System.Net;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Academics.AcademicsTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Academics;

/// <summary>
/// Spec 0013 AC-7: aynı sınıfın başka öğretmeni ödevin teslim durumunu ve değerlendirmenin puanlarını görür (değiştiremez
/// işaretiyle) ama değiştirmeye çalışırsa 403 alır ve hiçbir şey değişmez. Oluşturan öğretmen sınıftan alınınca kaydı
/// değiştiremez (404) ve yeni öğretmen de değiştiremez (403).
/// </summary>
[Trait("Category", "Integration")]
public sealed class AcademicsOwnershipTests
{
    private readonly TestDatabase database;

    public AcademicsOwnershipTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task OtherTeacherOfSameClass_Reads_ButSaveIs403AndChangesNothing()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid secondId, _, string second) = await TeacherAsync(factory, client, w.Class.School.TenantId, w.Class.School.BranchId);
        await AssignAsync(client, w.Class.Founder, w.Class.ClassroomId, w.Class.TeacherId, secondId);
        Guid assignment = await AssignedAsync(client, w.Class.Teacher, w.Class.ClassroomId);
        Guid assessment = await OpenedAsync(client, w.Class.Teacher, w.Class.ClassroomId);
        (await SaveSubmissionsAsync(client, w.Class.Teacher, assignment, (w.Ali, "Submitted"))).Status.ShouldBe(HttpStatusCode.OK);
        (await SaveGradesAsync(client, w.Class.Teacher, assessment, (w.Ali, 85))).Status.ShouldBe(HttpStatusCode.OK);

        var (readSubmissions, submissions, _) = await SubmissionsAsync(client, second, assignment);
        var (readGrades, grades, _) = await GradesAsync(client, second, assessment);
        readSubmissions.ShouldBe(HttpStatusCode.OK);
        submissions.GetProperty("canEdit").GetBoolean().ShouldBeFalse();
        submissions.GetProperty("createdByName").GetString().ShouldBe("Test Kişi");
        ByStudent(submissions, "status")[w.Ali].ShouldBe("Submitted");
        readGrades.ShouldBe(HttpStatusCode.OK);
        grades.GetProperty("canEdit").GetBoolean().ShouldBeFalse();
        ByStudent(grades, "score")[w.Ali].ShouldBe("85");

        (await SaveSubmissionsAsync(client, second, assignment, (w.Ali, "NotSubmitted"), (w.Ayse, "NotSubmitted"))).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await SaveGradesAsync(client, second, assessment, (w.Ali, 10), (w.Ayse, 20))).Status.ShouldBe(HttpStatusCode.Forbidden);

        Dictionary<Guid, string?> statuses = ByStudent((await SubmissionsAsync(client, w.Class.Teacher, assignment)).Body, "status");
        statuses[w.Ali].ShouldBe("Submitted");
        statuses[w.Ayse].ShouldBe("Unmarked");
        Dictionary<Guid, string?> scores = ByStudent((await GradesAsync(client, w.Class.Teacher, assessment)).Body, "score");
        scores[w.Ali].ShouldBe("85");
        scores[w.Ayse].ShouldBeNull();
    }

    [Fact]
    public async Task OwnerRemovedFromClass_NobodyCanChange()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid secondId, _, string second) = await TeacherAsync(factory, client, w.Class.School.TenantId, w.Class.School.BranchId);
        Guid assignment = await AssignedAsync(client, w.Class.Teacher, w.Class.ClassroomId);
        await AssignAsync(client, w.Class.Founder, w.Class.ClassroomId, secondId);

        (await SaveSubmissionsAsync(client, w.Class.Teacher, assignment, (w.Ali, "Submitted"))).Status.ShouldBe(HttpStatusCode.NotFound);
        (await SubmissionsAsync(client, second, assignment)).Body.GetProperty("canEdit").GetBoolean().ShouldBeFalse();
        (await SaveSubmissionsAsync(client, second, assignment, (w.Ali, "Submitted"))).Status.ShouldBe(HttpStatusCode.Forbidden);
    }
}
