using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Tests.Integration.Attendance;
using OkulYonetim.Tests.Integration.Billing;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Dashboard.DashboardTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Dashboard;

/// <summary>
/// Spec 0017 AC-3 (devamsızlık kartı): bilinen veride (Merkez 5-A yoklaması alındı — Ali yok, Ayşe geç —, 5-B alınmadı; öğrencisiz 5-C ve
/// öğrencisi olan ama pasif 5-D sayılmaz) alınan/alınmayan sınıf sayısı, alınmayanların listesi (şube, sınıf, öğretmen), yok/geç toplamı ve
/// yok öğrenci listesi doğru; "bugün" Türkiye günüdür — UTC'de hâlâ 20 Kasım olan 22:30'da (Türkiye 21 Kasım 01:30) dünkü yoklama sayılmaz,
/// bugünkü sayılır; takvimde tatil olan gün ve dönem dışı gün "okul günü değil"; sınıfı olmayan okulda sayılan sınıf yok.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AttendanceCardTests
{
    private readonly TestDatabase database;

    public AttendanceCardTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task KnownData_CountsListsAndTotalsAreRight_EmptyAndPassiveClassroomsNotCounted()
    {
        using OkulApiFactory factory = DashboardFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await DashboardWorldAsync(factory, client);
        AttendanceClass c = w.School.Class;
        await CreateClassroomAsync(client, c.Founder, c.TermId, c.School.BranchId, c.LevelId, "5-C");
        Guid passive = await CreateClassroomAsync(client, c.Founder, c.TermId, c.School.BranchId, c.LevelId, "5-D");
        await StudentAsync(client, c, "Deniz Pasif", passive);
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/classrooms/{passive}/deactivate", c.Founder)).ShouldBe(HttpStatusCode.OK);
        await SavedAsync(client, c.Teacher, c.ClassroomId, [(w.Ali, "Absent"), (w.School.Ayse, "Late")]);

        var (status, card, raw) = await AttendanceCardAsync(client, w.Founder);

        status.ShouldBe(HttpStatusCode.OK, raw);
        card.GetProperty("today").GetString().ShouldBe("2026-11-20");
        card.GetProperty("isSchoolDay").GetBoolean().ShouldBeTrue();
        card.GetProperty("classroomCount").GetInt32().ShouldBe(2);
        card.GetProperty("takenCount").GetInt32().ShouldBe(1);
        card.GetProperty("notTakenCount").GetInt32().ShouldBe(1);
        JsonElement notTaken = card.GetProperty("notTaken").EnumerateArray().ShouldHaveSingleItem();
        notTaken.GetProperty("classroomName").GetString().ShouldBe("5-B");
        notTaken.GetProperty("branchName").GetString().ShouldBe("Merkez");
        notTaken.GetProperty("teacherNames").EnumerateArray().Select(n => n.GetString()).ShouldBe(["Test Kişi"]);
        card.GetProperty("absentCount").GetInt32().ShouldBe(1);
        card.GetProperty("lateCount").GetInt32().ShouldBe(1);
        JsonElement absent = card.GetProperty("absentStudents").EnumerateArray().ShouldHaveSingleItem();
        absent.GetProperty("fullName").GetString().ShouldBe("Ali Yılmaz");
        absent.GetProperty("branchName").GetString().ShouldBe("Merkez");
        absent.GetProperty("classroomName").GetString().ShouldBe("5-A");
        raw.ShouldNotContain("Deniz Pasif");
    }

    [Fact]
    public async Task TodayIsTurkeyDay_LateNightUtcStillPreviousDay()
    {
        FakeTimeProvider clock = Clock();
        using OkulApiFactory factory = DashboardFactory(this.database, clock);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await DashboardWorldAsync(factory, client);
        AttendanceClass c = w.School.Class;
        await SavedAsync(client, c.Teacher, c.ClassroomId, [(w.Ali, "Absent")]);

        clock.SetUtcNow(new DateTimeOffset(2026, 11, 20, 22, 30, 0, TimeSpan.Zero));
        string founder = await BillingTestData.FounderTokenAsync(client, w);
        JsonElement night = (await AttendanceCardAsync(client, founder)).Body;
        night.GetProperty("today").GetString().ShouldBe("2026-11-21", "UTC'de 20 Kasım, Türkiye'de 21 Kasım 01:30");
        night.GetProperty("takenCount").GetInt32().ShouldBe(0, "dünkü (20 Kasım) yoklama bugün sayılmaz");
        night.GetProperty("absentCount").GetInt32().ShouldBe(0);

        string teacher = await TokenForSchoolAsync(client, c.TeacherEmail, c.School.TenantId);
        (await SaveAsync(client, teacher, c.ClassroomId, [(w.School.Ayse, "Absent"), (w.Ali, "Present")], day: "2026-11-21")).Status.ShouldBe(HttpStatusCode.OK);
        JsonElement after = (await AttendanceCardAsync(client, founder)).Body;
        after.GetProperty("takenCount").GetInt32().ShouldBe(1);
        Names(after.GetProperty("absentStudents")).ShouldBe(["Ayşe Kaya"]);
    }

    [Fact]
    public async Task Holiday_OutsideTerm_AndNoClassrooms()
    {
        using OkulApiFactory factory = DashboardFactory(this.database);
        using HttpClient client = factory.CreateClient();
        AttendanceClass holiday = await ClassAsync(factory, client);
        await StudentAsync(client, holiday, "Ece Tatil");
        await CreateCalendarEntryAsync(client, holiday.Founder, holiday.TermId, "Öğretmenler günü", "Holiday", "2026-11-20", "2026-11-20");
        JsonElement onHoliday = (await AttendanceCardAsync(client, holiday.Founder)).Body;
        onHoliday.GetProperty("isSchoolDay").GetBoolean().ShouldBeFalse();
        onHoliday.GetProperty("classroomCount").GetInt32().ShouldBe(0);

        AttendanceClass future = await ClassAsync(factory, client, termStart: "2027-02-01", termEnd: "2027-06-15");
        await StudentAsync(client, future, "Efe Gelecek");
        (await AttendanceCardAsync(client, future.Founder)).Body.GetProperty("isSchoolDay").GetBoolean().ShouldBeFalse("yaklaşan dönem okul günü değil");

        (SchoolSeed empty, string emptyFounder) = await SchoolAsync(factory, client);
        await CreateTermAsync(client, emptyFounder, "Güz", "2026-09-01", "2027-01-22");
        JsonElement none = (await AttendanceCardAsync(client, emptyFounder)).Body;
        none.GetProperty("isSchoolDay").GetBoolean().ShouldBeTrue();
        none.GetProperty("classroomCount").GetInt32().ShouldBe(0, $"{empty.TenantId}: sınıf yok");
    }
}
