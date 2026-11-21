using System.Net;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Academics.AcademicsTestData;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Academics;

/// <summary>
/// Spec 0013 AC-9 (izolasyon, S1): başka okulun ve başka şubenin öğretmeni bu okulun ödev ve değerlendirmelerini (sınıf ya da
/// kayıt kimliğiyle) okuyamaz ve yazamaz — 404, varlık sızmaz, hiçbir şey yazılmaz; başka okulun velisi bu okulun öğrencisinin
/// özetinde 404.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AcademicsIsolationTests
{
    private readonly TestDatabase database;

    public AcademicsIsolationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task OtherSchoolAndOtherBranchTeachers_404_NothingWritten()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        CommunicationWorld otherSchool = await WorldAsync(factory, client);
        Guid avcilar = await CreateBranchAsync(client, w.Class.Founder, "Avcılar");
        Guid avcilarClass = await CreateClassroomAsync(client, w.Class.Founder, w.Class.TermId, avcilar, w.Class.LevelId, "6-A");
        (Guid avcilarTeacherId, _, string avcilarTeacher) = await TeacherAsync(factory, client, w.Class.School.TenantId, avcilar);
        await AssignAsync(client, w.Class.Founder, avcilarClass, avcilarTeacherId);
        Guid assignment = await AssignedAsync(client, w.Class.Teacher, w.Class.ClassroomId);
        Guid assessment = await OpenedAsync(client, w.Class.Teacher, w.Class.ClassroomId);

        foreach (string token in new[] { otherSchool.Class.Teacher, avcilarTeacher })
        {
            (await AssignmentsAsync(client, token, w.Class.ClassroomId)).Status.ShouldBe(HttpStatusCode.NotFound);
            (await CreateAssignmentAsync(client, token, w.Class.ClassroomId, Homework())).Status.ShouldBe(HttpStatusCode.NotFound);
            (await SubmissionsAsync(client, token, assignment)).Status.ShouldBe(HttpStatusCode.NotFound);
            (await SaveSubmissionsAsync(client, token, assignment, (w.Ali, "NotSubmitted"))).Status.ShouldBe(HttpStatusCode.NotFound);
            (await AssessmentsAsync(client, token, w.Class.ClassroomId)).Status.ShouldBe(HttpStatusCode.NotFound);
            (await CreateAssessmentAsync(client, token, w.Class.ClassroomId, Exam())).Status.ShouldBe(HttpStatusCode.NotFound);
            (await GradesAsync(client, token, assessment)).Status.ShouldBe(HttpStatusCode.NotFound);
            (await SaveGradesAsync(client, token, assessment, (w.Ali, 10))).Status.ShouldBe(HttpStatusCode.NotFound);
        }

        (await SummaryAsync(client, otherSchool.AliMother.Token, w.Ali)).Status.ShouldBe(HttpStatusCode.NotFound, "başka okulun velisi");
        (await AssignmentsAsync(client, w.Class.Teacher, otherSchool.Class.ClassroomId)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await AssignmentCountAsync(factory, w.Class.ClassroomId)).ShouldBe(1);
        (await AssessmentCountAsync(factory, w.Class.ClassroomId)).ShouldBe(1);
        ByStudent((await SubmissionsAsync(client, w.Class.Teacher, assignment)).Body, "status")[w.Ali].ShouldBe("Unmarked");
        ByStudent((await GradesAsync(client, w.Class.Teacher, assessment)).Body, "score")[w.Ali].ShouldBeNull();
    }
}
