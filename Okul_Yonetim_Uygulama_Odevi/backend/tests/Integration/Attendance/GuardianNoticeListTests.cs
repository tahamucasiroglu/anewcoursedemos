using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Tests.Integration.Account;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Attendance;

/// <summary>
/// Spec 0010 AC-9 (BR-2): veli yalnızca kendi çocuklarının bildirimlerini — farklı şubedeki kardeş dahil — yeniden eskiye
/// görür; liste en fazla 50; hoş geldin'deki okunmamış sayısı doğru, "okundu" sonrası sıfır; bildirimi olmayan velinin
/// listesi boş; başka velinin bildirimi görünmez; veli olmayan 403.
/// </summary>
[Trait("Category", "Integration")]
public sealed class GuardianNoticeListTests
{
    private readonly TestDatabase database;

    public GuardianNoticeListTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Guardian_SeesOwnChildrenAcrossBranches_NewestFirst_UnreadCountResetsOnRead()
    {
        FakeTimeProvider time = new(Morning);
        using OkulApiFactory factory = Factory(this.database, new CapturingOutbox(), time);
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);
        Guid avcilar = await CreateBranchAsync(client, c.Founder, "Avcılar");
        Guid avcilarClass = await CreateClassroomAsync(client, c.Founder, c.TermId, avcilar, c.LevelId, "6-A");
        (Guid teacher2Id, _, string teacher2) = await TeacherAsync(factory, client, c.School.TenantId, avcilar);
        await AssignAsync(client, c.Founder, avcilarClass, teacher2Id);
        Guid older = await StudentAsync(client, c, "Ablası Ada");
        Guid younger = await CreateStudentAsync(client, c.Founder, NewStudent(avcilar, "Kardeşi Can", classroomId: avcilarClass));
        Guid stranger = await StudentAsync(client, c, "Yabancı Öğrenci");
        (string guardian, _, string email, _) = await NewGuardianAsync(client, c.Founder, c.School.TenantId, older);
        await LinkAsync(client, c.Founder, younger, Guardian(email));
        (string otherGuardian, _, _, _) = await NewGuardianAsync(client, c.Founder, c.School.TenantId, stranger);
        (await UnreadAsync(client, guardian)).ShouldBe(0);
        (await NoticesAsync(client, guardian)).Body.GetArrayLength().ShouldBe(0, "Henüz bildirim yok");

        await SavedAsync(client, c.Teacher, c.ClassroomId, [(older, "Absent"), (stranger, "Absent")]);
        time.Advance(TimeSpan.FromMinutes(5));
        await SavedAsync(client, teacher2, avcilarClass, [(younger, "Absent")]);

        var (status, list, raw) = await NoticesAsync(client, guardian);
        status.ShouldBe(HttpStatusCode.OK);
        list.EnumerateArray().Select(n => n.GetProperty("studentName").GetString()).ShouldBe(["Kardeşi Can", "Ablası Ada"], "en yenisi üstte");
        raw.ShouldNotContain("Yabancı");
        (await UnreadAsync(client, guardian)).ShouldBe(2);
        (await NoticesAsync(client, otherGuardian)).Body.EnumerateArray().Select(n => n.GetProperty("studentId").GetGuid()).ShouldBe([stranger]);

        (await StatusAsync(client, HttpMethod.Post, "/api/notices/read", guardian)).ShouldBe(HttpStatusCode.NoContent);

        (await UnreadAsync(client, guardian)).ShouldBe(0);
        (await NoticesAsync(client, guardian)).Body.EnumerateArray().ShouldAllBe(n => n.GetProperty("isRead").GetBoolean());
        (await UnreadAsync(client, otherGuardian)).ShouldBe(1, "başka velinin okunmamışı etkilenmez");
    }

    [Fact]
    public async Task List_IsCappedAt50_NewestFirst_NonGuardianGets403()
    {
        using OkulApiFactory factory = Factory(this.database, new CapturingOutbox());
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);
        Guid ali = await StudentAsync(client, c, "Ali Yılmaz");
        (string guardian, Guid guardianId, _, _) = await NewGuardianAsync(client, c.Founder, c.School.TenantId, ali);
        await WithContextAsync(factory, context =>
        {
            for (int i = 0; i < 55; i++)
            {
                DateOnly day = new DateOnly(2026, 9, 1).AddDays(i);
                context.GuardianNotices.Add(GuardianNotice.Absent(c.School.TenantId, guardianId, ali, day, Morning.AddDays(-60 + i)));
            }

            return Task.CompletedTask;
        });

        JsonElement list = (await NoticesAsync(client, guardian)).Body;

        list.GetArrayLength().ShouldBe(50);
        list[0].GetProperty("day").GetString().ShouldBe("2026-10-25", "en yeni (55. gün)");
        list[49].GetProperty("day").GetString().ShouldBe("2026-09-06");
        (await UnreadAsync(client, guardian)).ShouldBe(55, "sayı listeyle sınırlı değil");
        (await StatusAsync(client, HttpMethod.Get, "/api/notices", c.Teacher)).ShouldBe(HttpStatusCode.Forbidden);
        (await StatusAsync(client, HttpMethod.Post, "/api/notices/read", c.Founder)).ShouldBe(HttpStatusCode.Forbidden);
        (await UnreadAsync(client, c.Teacher)).ShouldBe(0, "veli olmayana sayı 0");
    }
}
