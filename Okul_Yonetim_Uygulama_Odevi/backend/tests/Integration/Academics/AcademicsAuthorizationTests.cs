using System.Net;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Academics.AcademicsTestData;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Academics;

/// <summary>
/// Spec 0013 AC-9 (rol ve ilişki): öğretmen atanmadığı sınıfın ödev ve değerlendirmelerini okuyamaz ve yazamaz (404); öğretmen
/// rolü olmayan (veli, muhasebe, atanmamış kurucu) öğretmen işlemlerinde 403; veli bağlı olmadığı öğrencinin özetinde 404,
/// veli rolü olmayan veli özetinde 403; sınıftan alınan öğretmenin bir sonraki isteği 404, öğretmen rolü alınanınki 403,
/// pasifleştirilenin 401; girişsiz istek 401.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AcademicsAuthorizationTests
{
    private readonly TestDatabase database;

    public AcademicsAuthorizationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task UnassignedTeacher_404_ForEveryTeacherEndpoint_NothingWritten()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid assignment = await AssignedAsync(client, w.Class.Teacher, w.Class.ClassroomId);
        Guid assessment = await OpenedAsync(client, w.Class.Teacher, w.Class.ClassroomId);
        string other = w.OtherTeacher;

        (await AssignmentsAsync(client, other, w.Class.ClassroomId)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await CreateAssignmentAsync(client, other, w.Class.ClassroomId, Homework())).Status.ShouldBe(HttpStatusCode.NotFound);
        (await SubmissionsAsync(client, other, assignment)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await SaveSubmissionsAsync(client, other, assignment, (w.Ali, "Submitted"))).Status.ShouldBe(HttpStatusCode.NotFound);
        (await AssessmentsAsync(client, other, w.Class.ClassroomId)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await CreateAssessmentAsync(client, other, w.Class.ClassroomId, Exam())).Status.ShouldBe(HttpStatusCode.NotFound);
        (await GradesAsync(client, other, assessment)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await SaveGradesAsync(client, other, assessment, (w.Ali, 50))).Status.ShouldBe(HttpStatusCode.NotFound);
        (await SubmissionsAsync(client, w.Class.Teacher, Guid.NewGuid())).Status.ShouldBe(HttpStatusCode.NotFound);

        (await AssignmentCountAsync(factory, w.Class.ClassroomId)).ShouldBe(1);
        (await AssessmentCountAsync(factory, w.Class.ClassroomId)).ShouldBe(1);
        ByStudent((await SubmissionsAsync(client, w.Class.Teacher, assignment)).Body, "status")[w.Ali].ShouldBe("Unmarked");
        ByStudent((await GradesAsync(client, w.Class.Teacher, assessment)).Body, "score")[w.Ali].ShouldBeNull();
    }

    [Fact]
    public async Task NonTeacherRoles_403_GuardianSummaryRules_Anonymous401()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid assignment = await AssignedAsync(client, w.Class.Teacher, w.Class.ClassroomId);
        Guid assessment = await OpenedAsync(client, w.Class.Teacher, w.Class.ClassroomId);
        string accountantEmail = UniqueEmail("muhasebe");
        await AddMemberAsync(factory, accountantEmail, w.Class.School.TenantId, Role.Accounting, w.Class.School.BranchId);
        string accountant = await TokenForSchoolAsync(client, accountantEmail, w.Class.School.TenantId);

        foreach (string token in new[] { w.AliMother.Token, accountant, w.Class.Founder })
        {
            (await AssignmentsAsync(client, token, w.Class.ClassroomId)).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await CreateAssignmentAsync(client, token, w.Class.ClassroomId, Homework())).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await SubmissionsAsync(client, token, assignment)).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await SaveSubmissionsAsync(client, token, assignment, (w.Ali, "Submitted"))).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await CreateAssessmentAsync(client, token, w.Class.ClassroomId, Exam())).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await GradesAsync(client, token, assessment)).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await SaveGradesAsync(client, token, assessment, (w.Ali, 50))).Status.ShouldBe(HttpStatusCode.Forbidden);
        }

        (await SummaryAsync(client, w.AliMother.Token, w.Ayse)).Status.ShouldBe(HttpStatusCode.NotFound, "bağı olmayan öğrenci");
        var (gone, problem, _) = await SummaryAsync(client, w.AliMother.Token, w.Berk);
        gone.ShouldBe(HttpStatusCode.NotFound);
        Detail(problem).ShouldBe("Bu öğrenci artık görüntülenemiyor.");
        (await SummaryAsync(client, w.Class.Teacher, w.Ali)).Status.ShouldBe(HttpStatusCode.Forbidden, "veli rolü yok");
        (await SummaryAsync(client, accountant, w.Ali)).Status.ShouldBe(HttpStatusCode.Forbidden);

        foreach (string path in new[] { $"/api/academics/classrooms/{w.Class.ClassroomId}/assignments", $"/api/academics/assignments/{assignment}/submissions", $"/api/academics/assessments/{assessment}/grades", $"/api/my/children/{w.Ali}/academics" })
        {
            using HttpResponseMessage anonymous = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
            anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        (await AssignmentCountAsync(factory, w.Class.ClassroomId)).ShouldBe(1);
        (await AssessmentCountAsync(factory, w.Class.ClassroomId)).ShouldBe(1);
    }

    [Fact]
    public async Task RemovedRoleRevokedOrDeactivated_NextRequestRejected()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid second, _, string secondToken) = await TeacherAsync(factory, client, w.Class.School.TenantId, w.Class.School.BranchId);
        (Guid third, _, string thirdToken) = await TeacherAsync(factory, client, w.Class.School.TenantId, w.Class.School.BranchId);
        await AssignAsync(client, w.Class.Founder, w.Class.ClassroomId, w.Class.TeacherId, second, third);
        Guid mine = await AssignedAsync(client, w.Class.Teacher, w.Class.ClassroomId);

        await AssignAsync(client, w.Class.Founder, w.Class.ClassroomId, second, third);
        (await AssignmentsAsync(client, w.Class.Teacher, w.Class.ClassroomId)).Status.ShouldBe(HttpStatusCode.NotFound, "atamadan çıkarıldı");
        (await SaveSubmissionsAsync(client, w.Class.Teacher, mine, (w.Ali, "Submitted"))).Status.ShouldBe(HttpStatusCode.NotFound);

        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/members/{second}/roles", w.Class.Founder, new { roles = new[] { new { role = "Guardian", branchIds = new[] { w.Class.School.BranchId } } } }))
            .ShouldBe(HttpStatusCode.OK);
        (await AssignmentsAsync(client, secondToken, w.Class.ClassroomId)).Status.ShouldBe(HttpStatusCode.Forbidden, "öğretmen rolü alındı");

        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/members/{third}/deactivate", w.Class.Founder)).ShouldBe(HttpStatusCode.OK);
        (await AssignmentsAsync(client, thirdToken, w.Class.ClassroomId)).Status.ShouldBe(HttpStatusCode.Unauthorized, "pasif üyelikte oturum geçmez");
        (await CreateAssignmentAsync(client, thirdToken, w.Class.ClassroomId, Homework())).Status.ShouldBe(HttpStatusCode.Unauthorized);
        (await AssignmentCountAsync(factory, w.Class.ClassroomId)).ShouldBe(1);
    }
}
