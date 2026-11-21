using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Tests.Integration.Attendance;
using OkulYonetim.Tests.Integration.Billing;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Billing.BillingTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;
using static OkulYonetim.Tests.Integration.Dashboard.DashboardTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Dashboard;

/// <summary>
/// Spec 0017 AC-2 (izolasyon; S1): B okulunda yoklama, plan, tahsilat ve duyuru dolu iken A okulunun kurucusunun kartlarında, raporlarında ve
/// dosyalarında B'nin hiçbir sınıfı, öğrencisi, tahsilatı ya da duyurusu yoktur; B'nin şube kimliğiyle süzgeç 404 (kart, rapor, dosya); tek
/// şube süzgecinde başka şubenin yoklama, borç ve şube/sınıf/öğrenci kapsamlı duyuru satırları yoktur, okul geneli duyuru vardır.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DashboardIsolationTests
{
    private readonly TestDatabase database;

    public DashboardIsolationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task OtherSchoolInvisible_OtherBranchFilteredOut_ForeignBranch404()
    {
        FakeTimeProvider clock = Clock();
        using OkulApiFactory factory = DashboardFactory(this.database, clock);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await DashboardWorldAsync(factory, client);
        CommunicationWorld s = w.School;
        AttendanceClass a = s.Class;

        Guid k1 = await CreateClassroomAsync(client, a.Founder, a.TermId, w.OtherBranchId, a.LevelId, "K-1");
        (Guid kTeacherId, _, string kTeacher) = await TeacherAsync(factory, client, w.TenantId, w.OtherBranchId);
        await AssignAsync(client, a.Founder, k1, kTeacherId);
        Guid kerem = await CreateStudentAsync(client, a.Founder, NewStudent(w.OtherBranchId, "Kerem Kadıköy", classroomId: k1));
        await CommunicationTestData.ParentAsync(client, a, kerem);

        AttendanceClass b = await ClassAsync(factory, client);
        Guid deniz = await StudentAsync(client, b, "Deniz Başka");
        await CommunicationTestData.ParentAsync(client, b, deniz);
        Guid bFee = Id(await CreateTariffAsync(client, b.Founder, "B Ücreti", 5_000m));
        Guid bPlan = await PlanAsync(client, b.Founder, PlanBody(deniz, bFee, installmentCount: 1, firstDueDate: "2026-12-01"));
        (_, long bReceipt) = await PaidAsync(client, b.Founder, bPlan, 777m);
        await PublishedAsync(client, b.Founder, Plain("School", null, title: "B duyurusu"));
        await SavedAsync(client, b.Teacher, b.ClassroomId, [(deniz, "Absent")]);

        await PlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId, installmentCount: 1, firstDueDate: "2026-12-01"));
        Guid cemPlan = await PlanAsync(client, w.Founder, PlanBody(w.Cem, w.FeeId, installmentCount: 1, firstDueDate: "2026-12-01"));
        await PaidAsync(client, w.Founder, cemPlan, 500m);
        await PublishedAsync(client, w.Founder, Plain("School", null, title: "Okul geneli"));
        await PublishedAsync(client, w.Founder, Plain("Branch", w.BranchId, title: "Merkez duyurusu"));
        await PublishedAsync(client, w.Founder, Plain("Branch", w.OtherBranchId, title: "Kadıköy duyurusu"));
        await PublishedAsync(client, w.Founder, Plain("Classroom", k1, title: "K-1 duyurusu"));
        await PublishedAsync(client, w.Founder, Plain("Student", w.Cem, title: "Cem duyurusu"));
        await SavedAsync(client, a.Teacher, a.ClassroomId, [(w.Ali, "Absent")]);
        await SavedAsync(client, kTeacher, k1, [(kerem, "Absent")]);

        string[] foreign = ["Deniz Başka", "B duyurusu", deniz.ToString(), b.ClassroomId.ToString()];
        string all = (await AttendanceCardAsync(client, w.Founder)).Raw + (await AnnouncementsCardAsync(client, w.Founder)).Raw
            + (await AttendanceReportAsync(client, w.Founder, "startDate=2026-11-01&endDate=2026-11-20")).Raw
            + (await PaymentsReportAsync(client, w.Founder, "startDate=2026-11-01&endDate=2026-11-20")).Raw
            + (await BalancesReportAsync(client, w.Founder)).Raw
            + (await ExportAsync(client, w.Founder, "attendance", "startDate=2026-11-01&endDate=2026-11-20")).Text
            + (await ExportAsync(client, w.Founder, "payments", "startDate=2026-11-01&endDate=2026-11-20")).Text
            + (await ExportAsync(client, w.Founder, "balances")).Text;
        foreach (string text in foreign)
        {
            all.ShouldNotContain(text);
        }

        all.ShouldContain("Kerem Kadıköy");
        JsonElement payments = (await PaymentsReportAsync(client, w.Founder, "startDate=2026-11-01&endDate=2026-11-20")).Body;
        payments.GetProperty("items").EnumerateArray().Select(p => p.GetProperty("amount").GetDecimal()).ShouldBe([500m]);
        bReceipt.ShouldBe(1);

        foreach (string path in AllPaths.Where(p => !p.Contains("/export", StringComparison.Ordinal)))
        {
            string url = path + (path.Contains('?', StringComparison.Ordinal) ? "&" : "?") + $"branchId={b.School.BranchId}";
            (await StatusAsync(client, HttpMethod.Get, url, w.Founder)).ShouldBe(HttpStatusCode.NotFound, url);
        }

        (await ExportAsync(client, w.Founder, "payments", $"startDate=2026-11-01&endDate=2026-11-20&branchId={b.School.BranchId}")).Status.ShouldBe(HttpStatusCode.NotFound);
        (await ExportAsync(client, w.Founder, "balances", $"branchId={b.School.BranchId}")).Status.ShouldBe(HttpStatusCode.NotFound);
        (await ExportAuditAsync(factory, w.TenantId)).Count.ShouldBe(3, "yalnızca başarılı üç dışa aktarma");

        JsonElement merkezAttendance = (await AttendanceCardAsync(client, w.Founder, w.BranchId)).Body;
        merkezAttendance.GetProperty("classroomCount").GetInt32().ShouldBe(2);
        Names(merkezAttendance.GetProperty("absentStudents")).ShouldBe(["Ali Yılmaz"]);
        JsonElement kadikoyAttendance = (await AttendanceCardAsync(client, w.Founder, w.OtherBranchId)).Body;
        kadikoyAttendance.GetProperty("classroomCount").GetInt32().ShouldBe(1);
        Names(kadikoyAttendance.GetProperty("absentStudents")).ShouldBe(["Kerem Kadıköy"]);
        Names((await AttendanceReportAsync(client, w.Founder, $"startDate=2026-11-20&endDate=2026-11-20&branchId={w.BranchId}")).Body.GetProperty("items")).ShouldBe(["Ali Yılmaz"]);

        Names((await AnnouncementsCardAsync(client, w.Founder, w.BranchId)).Body.GetProperty("top"), "title").Order(StringComparer.Ordinal)
            .ShouldBe(["Merkez duyurusu", "Okul geneli"]);
        Names((await AnnouncementsCardAsync(client, w.Founder, w.OtherBranchId)).Body.GetProperty("top"), "title").Order(StringComparer.Ordinal)
            .ShouldBe(["Cem duyurusu", "K-1 duyurusu", "Kadıköy duyurusu", "Okul geneli"]);

        Names((await PaymentsReportAsync(client, w.Founder, $"startDate=2026-11-01&endDate=2026-11-20&branchId={w.BranchId}")).Body.GetProperty("items"), "studentName").ShouldBeEmpty();
        Names((await BalancesReportAsync(client, w.Founder, $"branchId={w.BranchId}")).Body.GetProperty("items")).ShouldBe(["Ali Yılmaz"]);

        clock.SetUtcNow(new DateTimeOffset(2026, 12, 5, 9, 0, 0, TimeSpan.Zero));
        string founder = await FounderTokenAsync(client, w);
        Names((await OverdueCardAsync(client, founder)).Body.GetProperty("top")).ShouldBe(["Ali Yılmaz", "Cem Arslan"]);
        Names((await OverdueCardAsync(client, founder, w.BranchId)).Body.GetProperty("top")).ShouldBe(["Ali Yılmaz"]);
        Names((await OverdueCardAsync(client, founder, w.OtherBranchId)).Body.GetProperty("top")).ShouldBe(["Cem Arslan"]);
        (await OverdueCardAsync(client, founder)).Raw.ShouldNotContain("Deniz Başka");
    }
}
