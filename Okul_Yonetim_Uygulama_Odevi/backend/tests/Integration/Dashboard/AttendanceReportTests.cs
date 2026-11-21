using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Tests.Integration.Attendance;
using OkulYonetim.Tests.Integration.Billing;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Dashboard.DashboardTestData;

namespace OkulYonetim.Tests.Integration.Dashboard;

/// <summary>
/// Spec 0017 AC-7 (devamsızlık raporu): 16–20 Kasım her gün alınan yoklamalardan öğrenci başına yok/geç gün sayıları doğrudur; aralık dışındaki
/// gün sayılmaz, sınır günleri sayılır; sınıf süzgeci ve "en az 3 gün yok" eşiği doğru öğrencileri bırakır; sıra yok sayısına göre azalan,
/// eşitlikte Türkçe ad sırası (C &lt; Ç, I &lt; İ, O &lt; Ö, S &lt; Ş, U &lt; Ü); hiç yok/geç kaydı olmayan öğrenci listede yok; okul no sütunu dolu.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AttendanceReportTests
{
    private static readonly string[] TurkishNames = ["Ümit Er", "Şule Er", "Ömer Er", "İlker Er", "Çağrı Er", "Umut Er", "Sinan Er", "Ozan Er", "Irmak Er", "Can Er"];
    private readonly TestDatabase database;

    public AttendanceReportTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task DayCounts_RangeBoundaries_Filters_ThresholdAndTurkishOrder()
    {
        FakeTimeProvider clock = Clock(new DateTimeOffset(2026, 11, 16, 9, 0, 0, TimeSpan.Zero));
        using OkulApiFactory factory = DashboardFactory(this.database, clock);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await DashboardWorldAsync(factory, client);
        AttendanceClass c = w.School.Class;
        await AssignAsync(client, c.Founder, w.School.OtherClassroomId, c.TeacherId, w.School.OtherTeacherId);
        Dictionary<string, Guid> named = [];
        foreach (string name in TurkishNames)
        {
            named[name] = await Students.StudentsTestData.CreateStudentAsync(
                client, c.Founder, Students.StudentsTestData.NewStudent(c.School.BranchId, name, schoolNumber: "N" + named.Count, classroomId: c.ClassroomId));
        }

        await DayAsync(clock, client, c, 16, c.ClassroomId, (w.Ali, "Absent"), (w.School.Ayse, "Absent"));
        await DayAsync(clock, client, c, 17, c.ClassroomId, (w.Ali, "Absent"), (w.School.Ayse, "Late"));
        await DayAsync(clock, client, c, 18, c.ClassroomId, (w.Ali, "Absent"));
        await DayAsync(clock, client, c, 18, w.School.OtherClassroomId, (w.School.Berk, "Absent"));
        await DayAsync(clock, client, c, 19, c.ClassroomId, [.. named.Values.Select(id => (id, "Absent"))]);
        await DayAsync(clock, client, c, 20, c.ClassroomId, (w.Ali, "Late"));
        string founder = await BillingTestData.FounderTokenAsync(client, w);

        var (status, report, raw) = await AttendanceReportAsync(client, founder, "startDate=2026-11-16&endDate=2026-11-20");

        status.ShouldBe(HttpStatusCode.OK, raw);
        JsonElement items = report.GetProperty("items");
        Names(items).ShouldBe(["Ali Yılmaz", "Ayşe Kaya", "Berk Demir", "Can Er", "Çağrı Er", "Irmak Er", "İlker Er", "Ozan Er", "Ömer Er", "Sinan Er", "Şule Er", "Umut Er", "Ümit Er"]);
        items[0].GetProperty("absentDays").GetInt32().ShouldBe(3);
        items[0].GetProperty("lateDays").GetInt32().ShouldBe(1);
        items[1].GetProperty("absentDays").GetInt32().ShouldBe(1);
        items[1].GetProperty("lateDays").GetInt32().ShouldBe(1);
        items[3].GetProperty("schoolNumber").GetString().ShouldBe("N9");
        items[3].GetProperty("classroomName").GetString().ShouldBe("5-A");
        report.GetProperty("totalCount").GetInt32().ShouldBe(13);
        report.GetProperty("totals").GetProperty("absentDays").GetInt32().ShouldBe(15);
        report.GetProperty("totals").GetProperty("lateDays").GetInt32().ShouldBe(2);

        JsonElement boundary = (await AttendanceReportAsync(client, founder, "startDate=2026-11-17&endDate=2026-11-18")).Body.GetProperty("items");
        Names(boundary).ShouldBe(["Ali Yılmaz", "Berk Demir", "Ayşe Kaya"]);
        boundary[0].GetProperty("absentDays").GetInt32().ShouldBe(2, "16 Kasım aralık dışında, 17 ve 18 sınırda");
        boundary[2].GetProperty("absentDays").GetInt32().ShouldBe(0);
        boundary[2].GetProperty("lateDays").GetInt32().ShouldBe(1);

        Names((await AttendanceReportAsync(client, founder, "startDate=2026-11-16&endDate=2026-11-20&minAbsent=3")).Body.GetProperty("items")).ShouldBe(["Ali Yılmaz"]);
        Names((await AttendanceReportAsync(client, founder, $"startDate=2026-11-16&endDate=2026-11-20&classroomId={w.School.OtherClassroomId}")).Body.GetProperty("items")).ShouldBe(["Berk Demir"]);
        (await AttendanceReportAsync(client, founder, $"startDate=2026-11-16&endDate=2026-11-20&branchId={w.OtherBranchId}")).Body.GetProperty("totalCount").GetInt32().ShouldBe(0);
        JsonElement none = (await AttendanceReportAsync(client, founder, "startDate=2026-11-01&endDate=2026-11-15")).Body;
        none.GetProperty("totalCount").GetInt32().ShouldBe(0);
        none.GetProperty("items").GetArrayLength().ShouldBe(0);
    }

    private static async Task DayAsync(FakeTimeProvider clock, HttpClient client, AttendanceClass c, int day, Guid classroomId, params (Guid StudentId, string Status)[] marks)
    {
        clock.SetUtcNow(new DateTimeOffset(2026, 11, day, 9, 0, 0, TimeSpan.Zero));
        string teacher = await TokenForSchoolAsync(client, c.TeacherEmail, c.School.TenantId);
        var (status, _, raw) = await SaveAsync(client, teacher, classroomId, marks, day: $"2026-11-{day:00}");
        status.ShouldBe(HttpStatusCode.OK, raw);
    }
}
