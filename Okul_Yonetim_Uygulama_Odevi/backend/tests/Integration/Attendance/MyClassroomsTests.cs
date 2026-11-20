using System.Net;
using System.Text.Json;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Account;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Attendance;

/// <summary>
/// Spec 0010 AC-2: öğretmen yalnızca atandığı etkin sınıfları (şube ve ad sırasıyla) bugünkü durumuyla görür — atanmadığı,
/// pasifleşmiş ve dönemi bitmiş sınıf listede yok; yoklama kaydedilince "alındı" olur; atanmış sınıfı olmayan öğretmenin
/// listesi boştur ("Size atanmış sınıf yok" ekranda).
/// </summary>
[Trait("Category", "Integration")]
public sealed class MyClassroomsTests
{
    private readonly TestDatabase database;

    public MyClassroomsTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Teacher_SeesOnlyAssignedActiveCurrentClassrooms_WithTodayStatus()
    {
        using OkulApiFactory factory = Factory(this.database, new CapturingOutbox());
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);
        Guid avcilar = await CreateBranchAsync(client, c.Founder, "Avcılar");
        await AddMemberAsync(factory, c.TeacherEmail, c.School.TenantId, Role.Teacher, avcilar);
        Guid secondClass = await CreateClassroomAsync(client, c.Founder, c.TermId, c.School.BranchId, c.LevelId, "5-B");
        Guid avcilarClass = await CreateClassroomAsync(client, c.Founder, c.TermId, avcilar, c.LevelId, "6-A");
        Guid notMine = await CreateClassroomAsync(client, c.Founder, c.TermId, c.School.BranchId, c.LevelId, "5-C");
        Guid closed = await CreateClassroomAsync(client, c.Founder, c.TermId, c.School.BranchId, c.LevelId, "5-D");
        Guid pastTerm = await CreateTermAsync(client, c.Founder, "Geçen yıl", "2025-09-01", "2026-06-19");
        Guid oldClass = await CreateClassroomAsync(client, c.Founder, pastTerm, c.School.BranchId, c.LevelId, "4-A");
        foreach (Guid id in new[] { secondClass, avcilarClass, closed, oldClass })
        {
            await AssignAsync(client, c.Founder, id, c.TeacherId);
        }

        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/classrooms/{closed}/deactivate", c.Founder)).ShouldBe(HttpStatusCode.OK);
        Guid student = await StudentAsync(client, c, "Ali Yılmaz", secondClass);

        var (status, list, _) = await CallAsync(client, HttpMethod.Get, "/api/attendance/classrooms", c.Teacher);
        status.ShouldBe(HttpStatusCode.OK);
        Rows(list).ShouldBe([(avcilarClass, "6-A", "Avcılar", false), (c.ClassroomId, "5-A", "Merkez", false), (secondClass, "5-B", "Merkez", false)]);
        Rows(list).ShouldNotContain(r => r.Id == notMine || r.Id == closed || r.Id == oldClass);

        await SavedAsync(client, c.Teacher, secondClass, [(student, "Present")]);

        var (_, after, _) = await CallAsync(client, HttpMethod.Get, "/api/attendance/classrooms", c.Teacher);
        Rows(after).Single(r => r.Id == secondClass).Taken.ShouldBeTrue();
        Rows(after).Where(r => r.Id != secondClass).ShouldAllBe(r => !r.Taken);
    }

    [Fact]
    public async Task TeacherWithoutAssignment_GetsEmptyList()
    {
        using OkulApiFactory factory = Factory(this.database, new CapturingOutbox());
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);
        (_, _, string lonely) = await TeacherAsync(factory, client, c.School.TenantId, c.School.BranchId);

        var (status, list, _) = await CallAsync(client, HttpMethod.Get, "/api/attendance/classrooms", lonely);

        status.ShouldBe(HttpStatusCode.OK);
        list.GetArrayLength().ShouldBe(0);
    }

    private static (Guid Id, string Name, string Branch, bool Taken)[] Rows(JsonElement list) =>
        [.. list.EnumerateArray().Select(r => (r.GetProperty("id").GetGuid(), r.GetProperty("name").GetString()!, r.GetProperty("branchName").GetString()!, r.GetProperty("takenToday").GetBoolean()))];
}
