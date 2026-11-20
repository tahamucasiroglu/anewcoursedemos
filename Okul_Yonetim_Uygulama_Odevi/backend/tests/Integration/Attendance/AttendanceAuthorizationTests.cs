using System.Net;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Account;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Attendance;

/// <summary>
/// Spec 0010 AC-10 (S1, S3): öğretmen atanmadığı sınıfın, başka şubenin ve başka okulun yoklamasını okuyamaz ve yazamaz
/// (404); öğretmen rolü olmayan (veli, muhasebe, atanmamış kurucu) yoklama uçlarında 403; atamadan çıkarılan öğretmenin
/// sonraki isteği 404, rolü alınan öğretmeninki 403, pasifleştirilen öğretmeninki 401; girişsiz istek 401; veli başka velinin
/// bildirimini göremez.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AttendanceAuthorizationTests
{
    private readonly TestDatabase database;

    public AttendanceAuthorizationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Teacher_CannotReachUnassignedOtherBranchOrOtherSchoolClassroom()
    {
        using OkulApiFactory factory = Factory(this.database, new CapturingOutbox());
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);
        AttendanceClass other = await ClassAsync(factory, client);
        Guid ali = await StudentAsync(client, c, "Ali Yılmaz");
        Guid veli = await StudentAsync(client, other, "Başka Okul");
        Guid unassigned = await CreateClassroomAsync(client, c.Founder, c.TermId, c.School.BranchId, c.LevelId, "5-B");
        Guid avcilar = await CreateBranchAsync(client, c.Founder, "Avcılar");
        Guid otherBranch = await CreateClassroomAsync(client, c.Founder, c.TermId, avcilar, c.LevelId, "6-A");
        await AssignAsync(client, c.Founder, otherBranch, (await TeacherAsync(factory, client, c.School.TenantId, avcilar)).UserId);

        foreach (Guid classroom in new[] { unassigned, otherBranch, other.ClassroomId })
        {
            (await TodayAsync(client, c.Teacher, classroom)).Status.ShouldBe(HttpStatusCode.NotFound);
            (await SaveAsync(client, c.Teacher, classroom, [(ali, "Absent"), (veli, "Absent")])).Status.ShouldBe(HttpStatusCode.NotFound);
        }

        (await SheetCountAsync(factory, other.ClassroomId)).ShouldBe(0);
        (await TodayAsync(client, c.Teacher, c.ClassroomId)).Status.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NonTeacherRoles_Get403_Anonymous401()
    {
        using OkulApiFactory factory = Factory(this.database, new CapturingOutbox());
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);
        Guid ali = await StudentAsync(client, c, "Ali Yılmaz");
        (string guardian, _, _, _) = await NewGuardianAsync(client, c.Founder, c.School.TenantId, ali);
        string accountantEmail = UniqueEmail("muhasebe");
        await AddMemberAsync(factory, accountantEmail, c.School.TenantId, Role.Accounting, c.School.BranchId);
        string accountant = await TokenForSchoolAsync(client, accountantEmail, c.School.TenantId);

        foreach (string token in new[] { guardian, accountant, c.Founder })
        {
            (await StatusAsync(client, HttpMethod.Get, "/api/attendance/classrooms", token)).ShouldBe(HttpStatusCode.Forbidden);
            (await TodayAsync(client, token, c.ClassroomId)).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await SaveAsync(client, token, c.ClassroomId, [(ali, "Absent")])).Status.ShouldBe(HttpStatusCode.Forbidden);
        }

        using HttpResponseMessage anonymous = await client.GetAsync(new Uri($"/api/attendance/classrooms/{c.ClassroomId}/today", UriKind.Relative), TestContext.Current.CancellationToken);
        anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        using HttpResponseMessage anonymousNotices = await client.GetAsync(new Uri("/api/notices", UriKind.Relative), TestContext.Current.CancellationToken);
        anonymousNotices.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await SheetCountAsync(factory, c.ClassroomId)).ShouldBe(0);
    }

    [Fact]
    public async Task RemovedFromClass_RoleRevoked_OrDeactivated_NextRequestRejected()
    {
        using OkulApiFactory factory = Factory(this.database, new CapturingOutbox());
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);
        Guid ali = await StudentAsync(client, c, "Ali Yılmaz");
        (Guid second, _, string secondToken) = await TeacherAsync(factory, client, c.School.TenantId, c.School.BranchId);
        (Guid third, _, string thirdToken) = await TeacherAsync(factory, client, c.School.TenantId, c.School.BranchId);
        await AssignAsync(client, c.Founder, c.ClassroomId, c.TeacherId, second, third);
        foreach (string token in new[] { c.Teacher, secondToken, thirdToken })
        {
            (await TodayAsync(client, token, c.ClassroomId)).Status.ShouldBe(HttpStatusCode.OK);
        }

        await AssignAsync(client, c.Founder, c.ClassroomId, second, third);
        (await TodayAsync(client, c.Teacher, c.ClassroomId)).Status.ShouldBe(HttpStatusCode.NotFound, "atamadan çıkarıldı");
        (await SaveAsync(client, c.Teacher, c.ClassroomId, [(ali, "Absent")])).Status.ShouldBe(HttpStatusCode.NotFound);

        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/members/{second}/roles", c.Founder, new { roles = new[] { new { role = "Guardian", branchIds = new[] { c.School.BranchId } } } }))
            .ShouldBe(HttpStatusCode.OK);
        (await TodayAsync(client, secondToken, c.ClassroomId)).Status.ShouldBe(HttpStatusCode.Forbidden, "öğretmen rolü alındı");
        (await SaveAsync(client, secondToken, c.ClassroomId, [(ali, "Absent")])).Status.ShouldBe(HttpStatusCode.Forbidden);

        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/members/{third}/deactivate", c.Founder)).ShouldBe(HttpStatusCode.OK);
        (await TodayAsync(client, thirdToken, c.ClassroomId)).Status.ShouldBe(HttpStatusCode.Unauthorized, "pasif üyelikte oturum geçmez");
        (await SaveAsync(client, thirdToken, c.ClassroomId, [(ali, "Absent")])).Status.ShouldBe(HttpStatusCode.Unauthorized);
        (await SheetCountAsync(factory, c.ClassroomId)).ShouldBe(0);
    }
}
