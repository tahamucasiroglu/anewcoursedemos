using System.Net;
using System.Text.Json;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Account;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Attendance;

/// <summary>
/// Spec 0010 AC-1: kurucu bir sınıfa o şubenin etkin öğretmenlerini atar ve kaldırır; başka şubenin öğretmeni, öğretmen rolü
/// olmayan ya da pasif kişi atanamaz (hata mesajı, hiçbir şey değişmez); kurucu olmayan 403, başka okulun sınıfı 404;
/// atama ve kaldırma audit'e (kim, sınıf, öğretmen, IP) yazılır.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ClassroomTeacherTests
{
    private const string Unavailable = "Seçilen kişi bu sınıfın şubesinde etkin öğretmen değil";
    private readonly TestDatabase database;

    public ClassroomTeacherTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Founder_AssignsAndRemovesBranchTeachers_Audited()
    {
        using OkulApiFactory factory = Factory(this.database, new CapturingOutbox());
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.41");
        AttendanceClass c = await ClassAsync(factory, client);
        (Guid second, _, _) = await TeacherAsync(factory, client, c.School.TenantId, c.School.BranchId);

        var (status, view, _) = await CallAsync(client, HttpMethod.Get, $"/api/admin/classrooms/{c.ClassroomId}/teachers", c.Founder);
        status.ShouldBe(HttpStatusCode.OK);
        view.GetProperty("classroomName").GetString().ShouldBe("5-A");
        view.GetProperty("branchName").GetString().ShouldBe("Merkez");
        UserIds(view.GetProperty("assigned")).ShouldBe([c.TeacherId]);
        UserIds(view.GetProperty("candidates")).ShouldBe([second], "atanmış olan adaylarda tekrar görünmez");

        var (both, bothView, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/classrooms/{c.ClassroomId}/teachers", c.Founder, new { teacherUserIds = new[] { c.TeacherId, second } });
        both.ShouldBe(HttpStatusCode.OK);
        UserIds(bothView.GetProperty("assigned")).ShouldBe([c.TeacherId, second], ignoreOrder: true);

        var (removed, removedView, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/classrooms/{c.ClassroomId}/teachers", c.Founder, new { teacherUserIds = new[] { second } });
        removed.ShouldBe(HttpStatusCode.OK);
        UserIds(removedView.GetProperty("assigned")).ShouldBe([second]);
        UserIds(removedView.GetProperty("candidates")).ShouldBe([c.TeacherId]);

        List<AuditEntry> audit = await AuditAsync(factory, c.School.TenantId, AuditAction.ClassroomTeacherAssigned, AuditAction.ClassroomTeacherRemoved);
        audit.Select(a => (a.Action, a.Detail)).ShouldBe(
            [
                (AuditAction.ClassroomTeacherAssigned, $"teacher={c.TeacherId:D}"),
                (AuditAction.ClassroomTeacherAssigned, $"teacher={second:D}"),
                (AuditAction.ClassroomTeacherRemoved, $"teacher={c.TeacherId:D}"),
            ],
            ignoreOrder: true,
            "sahte saat: üçü aynı anda");
        audit.ShouldAllBe(a => a.ActorUserId == c.School.FounderId && a.TargetType == "Classroom" && a.TargetId == c.ClassroomId && a.IpAddress == "203.0.113.41" && a.OccurredAt == Morning);
    }

    [Fact]
    public async Task OtherBranchTeacher_NonTeacher_OrInactiveMember_Rejected_NothingChanges()
    {
        using OkulApiFactory factory = Factory(this.database, new CapturingOutbox());
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);
        Guid avcilar = await CreateBranchAsync(client, c.Founder, "Avcılar");
        (Guid otherBranch, _, _) = await TeacherAsync(factory, client, c.School.TenantId, avcilar);
        Guid accountant = await AddMemberAsync(factory, UniqueEmail("muhasebe"), c.School.TenantId, Role.Accounting, c.School.BranchId);
        Guid guardian = await AddMemberAsync(factory, UniqueEmail("veli"), c.School.TenantId, Role.Guardian, c.School.BranchId);
        (Guid passive, _, _) = await TeacherAsync(factory, client, c.School.TenantId, c.School.BranchId);
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/members/{passive}/deactivate", c.Founder)).ShouldBe(HttpStatusCode.OK);
        (Guid valid, _, _) = await TeacherAsync(factory, client, c.School.TenantId, c.School.BranchId);

        foreach (Guid wrong in new[] { otherBranch, accountant, guardian, passive })
        {
            var (status, body, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/classrooms/{c.ClassroomId}/teachers", c.Founder, new { teacherUserIds = new[] { c.TeacherId, valid, wrong } });
            status.ShouldBe(HttpStatusCode.BadRequest);
            Detail(body).ShouldBe(Unavailable);
        }

        (await CallAsync(client, HttpMethod.Get, $"/api/admin/classrooms/{c.ClassroomId}/teachers", c.Founder)).Body.GetProperty("assigned")
            .EnumerateArray().Select(t => t.GetProperty("userId").GetGuid()).ShouldBe([c.TeacherId], "reddedilen istekte geçerli kişi de atanmadı");
        (await AuditAsync(factory, c.School.TenantId, AuditAction.ClassroomTeacherAssigned)).Count.ShouldBe(1, "yalnızca kurulumdaki atama");
        var (repeated, repeatedBody, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/classrooms/{c.ClassroomId}/teachers", c.Founder, new { teacherUserIds = new[] { valid, valid } });
        repeated.ShouldBe(HttpStatusCode.BadRequest);
        Messages(repeatedBody).ShouldContain("Aynı öğretmen bir kez seçilir");
    }

    [Fact]
    public async Task NonFounder_Gets403_OtherSchoolClassroom_Gets404()
    {
        using OkulApiFactory factory = Factory(this.database, new CapturingOutbox());
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);
        AttendanceClass other = await ClassAsync(factory, client);

        (await StatusAsync(client, HttpMethod.Get, $"/api/admin/classrooms/{c.ClassroomId}/teachers", c.Teacher)).ShouldBe(HttpStatusCode.Forbidden);
        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/classrooms/{c.ClassroomId}/teachers", c.Teacher, new { teacherUserIds = Array.Empty<Guid>() })).ShouldBe(HttpStatusCode.Forbidden);
        (await StatusAsync(client, HttpMethod.Get, $"/api/admin/classrooms/{other.ClassroomId}/teachers", c.Founder)).ShouldBe(HttpStatusCode.NotFound);
        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/classrooms/{other.ClassroomId}/teachers", c.Founder, new { teacherUserIds = Array.Empty<Guid>() })).ShouldBe(HttpStatusCode.NotFound);
        (await CallAsync(client, HttpMethod.Get, $"/api/admin/classrooms/{other.ClassroomId}/teachers", other.Founder)).Body.GetProperty("assigned").GetArrayLength().ShouldBe(1, "başka okulun ataması değişmedi");
    }

    private static Guid[] UserIds(JsonElement items) => [.. items.EnumerateArray().Select(x => x.GetProperty("userId").GetGuid())];
}
