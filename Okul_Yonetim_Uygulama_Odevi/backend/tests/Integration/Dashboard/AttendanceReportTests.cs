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

    /// <summary>
    /// Plan 0017 A-4 + review tur 1 R1-1/R1-2: sınıf değiştiren öğrencide süzgeç yoklamanın alındığı sınıfa uygulanır, satırda bugünkü sınıf
    /// görünür; aynı gün iki sınıfta kayıt varsa öğrenci-gün başına son güncellenen yoklama esastır (0010 kuralı) — en fazla bir gün, o "Var"
    /// ise hiç. Ali 16–17'de 5-A'da yok, sonra 5-B'ye geçer; Ayşe 18'de 5-A'da "Yok", 5-B'ye geçip aynı gün sonra "Geç"; Berk 19'da 5-B'de
    /// "Yok", 5-A'ya geçip aynı gün sonra "Var".
    /// </summary>
    [Fact]
    public async Task ClassroomChange_FilterUsesRecordedClassroom_SameDayLatestRecordWins()
    {
        FakeTimeProvider clock = Clock(new DateTimeOffset(2026, 11, 16, 9, 0, 0, TimeSpan.Zero));
        using OkulApiFactory factory = DashboardFactory(this.database, clock);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await DashboardWorldAsync(factory, client);
        AttendanceClass c = w.School.Class;
        Guid classA = c.ClassroomId;
        Guid classB = w.School.OtherClassroomId;
        await AssignAsync(client, c.Founder, classB, c.TeacherId, w.School.OtherTeacherId);

        await DayAsync(clock, client, c, 16, classA, (w.Ali, "Absent"));
        await DayAsync(clock, client, c, 17, classA, (w.Ali, "Absent"));
        await Academics.AcademicsTestData.MoveAsync(client, await BillingTestData.FounderTokenAsync(client, w), w.Ali, "Ali Yılmaz", c.School.BranchId, classB);
        await DayAsync(clock, client, c, 18, classA, (w.School.Ayse, "Absent"));
        await Academics.AcademicsTestData.MoveAsync(client, await BillingTestData.FounderTokenAsync(client, w), w.School.Ayse, "Ayşe Kaya", c.School.BranchId, classB);
        await DayAsync(clock, client, c, 18, classB, 10, (w.School.Ayse, "Late"));
        await DayAsync(clock, client, c, 19, classB, (w.School.Berk, "Absent"));
        await Academics.AcademicsTestData.MoveAsync(client, await BillingTestData.FounderTokenAsync(client, w), w.School.Berk, "Berk Demir", c.School.BranchId, classA);
        await DayAsync(clock, client, c, 19, classA, 10, (w.School.Berk, "Present"));
        string founder = await BillingTestData.FounderTokenAsync(client, w);
        const string Range = "startDate=2026-11-16&endDate=2026-11-20";

        var (status, all, raw) = await AttendanceReportAsync(client, founder, Range);
        status.ShouldBe(HttpStatusCode.OK, raw);
        JsonElement items = all.GetProperty("items");
        Names(items).ShouldBe(["Ali Yılmaz", "Ayşe Kaya"], "Berk'in 19 Kasım'ı sonra güncellenen \"Var\" kaydıyla sayılmaz");
        items[0].GetProperty("absentDays").GetInt32().ShouldBe(2);
        items[0].GetProperty("classroomName").GetString().ShouldBe("5-B", "satırda öğrencinin bugünkü sınıfı");
        items[1].GetProperty("absentDays").GetInt32().ShouldBe(0, "Ayşe'nin 18 Kasım'ında son kayıt \"Geç\"");
        items[1].GetProperty("lateDays").GetInt32().ShouldBe(1);
        all.GetProperty("totals").GetProperty("absentDays").GetInt32().ShouldBe(2);
        all.GetProperty("totals").GetProperty("lateDays").GetInt32().ShouldBe(1);

        JsonElement inA = (await AttendanceReportAsync(client, founder, $"{Range}&classroomId={classA}")).Body.GetProperty("items");
        Names(inA).ShouldBe(["Ali Yılmaz"], "5-A süzgeci 5-A'da alınan ve son kayıt olan günleri sayar");
        inA[0].GetProperty("absentDays").GetInt32().ShouldBe(2);
        inA[0].GetProperty("classroomName").GetString().ShouldBe("5-B");

        JsonElement inB = (await AttendanceReportAsync(client, founder, $"{Range}&classroomId={classB}")).Body.GetProperty("items");
        Names(inB).ShouldBe(["Ayşe Kaya"], "Ali'nin 5-A'daki günleri 5-B süzgecinde sayılmaz; Berk'in 5-B kaydı sonra güncellenen \"Var\"la geçersiz");
        inB[0].GetProperty("absentDays").GetInt32().ShouldBe(0);
        inB[0].GetProperty("lateDays").GetInt32().ShouldBe(1);
    }

    private static Task DayAsync(FakeTimeProvider clock, HttpClient client, AttendanceClass c, int day, Guid classroomId, params (Guid StudentId, string Status)[] marks) =>
        DayAsync(clock, client, c, day, classroomId, 9, marks);

    private static async Task DayAsync(FakeTimeProvider clock, HttpClient client, AttendanceClass c, int day, Guid classroomId, int hourUtc, params (Guid StudentId, string Status)[] marks)
    {
        clock.SetUtcNow(new DateTimeOffset(2026, 11, day, hourUtc, 0, 0, TimeSpan.Zero));
        string teacher = await TokenForSchoolAsync(client, c.TeacherEmail, c.School.TenantId);
        var (status, _, raw) = await SaveAsync(client, teacher, classroomId, marks, day: $"2026-11-{day:00}");
        status.ShouldBe(HttpStatusCode.OK, raw);
    }
}
