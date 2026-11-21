using System.Net;
using Microsoft.EntityFrameworkCore;
using OkulYonetim.Domain.Billing;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Tests.Integration.Billing;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Billing.BillingTestData;
using static OkulYonetim.Tests.Integration.Dashboard.DashboardTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Dashboard;

/// <summary>
/// Spec 0017 AC-11 (dışa aktarma güvenliği; S2/S6): <c>=</c>, <c>+</c>, <c>-</c>, <c>@</c> ile başlayan ad hücreleri <c>'</c> ile kaçışlanır,
/// tutar sütunları sayı kalır (sekme ve satır başı birim testinde — ad girişte kırpıldığı için uçtan girilemez); 10.000 satır kabul,
/// 10.001 "Sonuç çok büyük…" ile reddedilir; başarılı her dışa aktarma audit kaydı oluşturur (kim, rapor, süzgeçler, satır sayısı, ne zaman,
/// IP), reddedilen oluşturmaz; dosyada ve panelde veli e-postası, sağlık notu, alerji yok; loglarda öğrenci adı yok (yalnızca kimlikler).
/// </summary>
[Trait("Category", "Integration")]
public sealed class ReportExportSecurityTests
{
    private const string Ip = "203.0.113.17";
    private readonly TestDatabase database;

    public ReportExportSecurityTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task FormulaNamesEscaped_AmountsStayNumbers_NoPersonalDataInFilesPanelOrLogs()
    {
        using OkulApiFactory factory = DashboardFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await DashboardWorldAsync(factory, client);
        string[] formulas = ["=1+1 Ali", "+905551112233", "-5 Veli", "@SUM(A1:A2)"];
        foreach (string name in formulas)
        {
            Guid student = await CreateStudentAsync(client, w.Founder, NewStudent(w.BranchId, name, classroomId: w.School.Class.ClassroomId));
            await PaidAsync(client, w.Founder, await PlanAsync(client, w.Founder, PlanBody(student, w.FeeId, installmentCount: 1)), 10m);
        }

        Guid health = await CreateStudentAsync(client, w.Founder, NewStudent(w.BranchId, "Sağlık Öğrenci", classroomId: w.School.Class.ClassroomId, healthNote: "Astımilacı", allergy: "Fıstıkalerjisi"));
        await PaidAsync(client, w.Founder, await PlanAsync(client, w.Founder, PlanBody(health, w.FeeId, installmentCount: 1)), 25.5m);
        await SavedAsync(client, w.School.Class.Teacher, w.School.Class.ClassroomId, [(health, "Absent")]);

        ExportResponse payments = await ExportAsync(client, w.Founder, "payments", "startDate=2026-11-01&endDate=2026-11-20");
        ExportResponse balances = await ExportAsync(client, w.Founder, "balances");
        ExportResponse attendance = await ExportAsync(client, w.Founder, "attendance", "startDate=2026-11-01&endDate=2026-11-20");
        string[] paymentLines = Lines(payments.Bytes);
        foreach (string name in formulas)
        {
            paymentLines.ShouldContain(l => l.StartsWith("20.11.2026;", StringComparison.Ordinal) && l.Contains(";'" + name + ";Merkez;10,00;Nakit;", StringComparison.Ordinal), name);
            Lines(balances.Bytes).ShouldContain(l => l.StartsWith("'" + name + ";Merkez;5-A;", StringComparison.Ordinal) && l.Contains(";10000,00;10,00;9990,00;0,00;", StringComparison.Ordinal), name);
        }

        paymentLines.ShouldContain(l => l.Contains(";Sağlık Öğrenci;Merkez;25,50;", StringComparison.Ordinal));
        string everything = payments.Text + balances.Text + attendance.Text
            + (await AttendanceCardAsync(client, w.Founder)).Raw + (await OverdueCardAsync(client, w.Founder)).Raw + (await AnnouncementsCardAsync(client, w.Founder)).Raw
            + (await AttendanceReportAsync(client, w.Founder, "startDate=2026-11-01&endDate=2026-11-20")).Raw
            + (await PaymentsReportAsync(client, w.Founder, "startDate=2026-11-01&endDate=2026-11-20")).Raw
            + (await BalancesReportAsync(client, w.Founder)).Raw;
        everything.ShouldContain("Sağlık Öğrenci");
        foreach (string secret in new[] { "Astımilacı", "Fıstıkalerjisi", w.School.AliMother.Email, w.School.AyseMother.Email, w.AccountantEmail, "@ornek.test" })
        {
            everything.ShouldNotContain(secret, Case.Insensitive);
        }

        string log = factory.ReadLog();
        log.ShouldContain("Rapor dışa aktarıldı");
        log.ShouldContain(w.TenantId.ToString());
        foreach (string name in formulas.Append("Sağlık Öğrenci").Append("Ali Yılmaz"))
        {
            log.ShouldNotContain(name);
        }
    }

    [Fact]
    public async Task TenThousandRowsAccepted_TenThousandOneRejected_OnlySuccessAudited()
    {
        using OkulApiFactory factory = DashboardFactory(this.database);
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", Ip);
        BillingWorld w = await DashboardWorldAsync(factory, client);
        Guid planId = await PlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId, installmentCount: 1));
        await PaidAsync(client, w.Founder, planId, 1m);
        await SeedPaymentsAsync(factory, [planId], w.School.Class.School.FounderId, firstReceipt: 2, count: 9_999);
        const string query = "startDate=2026-11-01&endDate=2026-11-20";

        ExportResponse accepted = await ExportAsync(client, w.Founder, "payments", query);

        accepted.Status.ShouldBe(HttpStatusCode.OK, accepted.Text.Length > 500 ? accepted.Text[..500] : accepted.Text);
        Lines(accepted.Bytes).Length.ShouldBe(1 + 10_000 + 4);
        AuditEntry audit = (await ExportAuditAsync(factory, w.TenantId)).ShouldHaveSingleItem();
        audit.ActorUserId.ShouldBe(w.School.Class.School.FounderId);
        audit.IpAddress.ShouldBe(Ip);
        audit.OccurredAt.ShouldBe(Morning);
        audit.TargetType.ShouldBe("Report");
        audit.Detail.ShouldBe("report=Payments;range=2026-11-01..2026-11-20;rows=10000");

        await SeedPaymentsAsync(factory, [planId], w.School.Class.School.FounderId, firstReceipt: 10_001, count: 1);
        ExportResponse rejected = await ExportAsync(client, w.Founder, "payments", query);

        rejected.Status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(System.Text.Json.JsonDocument.Parse(rejected.Text).RootElement).ShouldBe("Sonuç çok büyük (en fazla 10.000 satır); tarih aralığını ya da süzgeci daraltın.");
        (await ExportAuditAsync(factory, w.TenantId)).Count.ShouldBe(1, "reddedilen dışa aktarma audit'e yazılmaz");

        (await ExportAsync(client, w.Founder, "balances", $"branchId={w.BranchId}")).Status.ShouldBe(HttpStatusCode.OK);
        (await ExportAuditAsync(factory, w.TenantId)).Select(a => a.Detail).ShouldContain($"report=Balances;branch={w.BranchId};overdueOnly=false;rows=1");
        (await StatusAsync(client, HttpMethod.Get, "/api/dashboard/reports/attendance?startDate=2026-11-01&endDate=2026-11-20&minAbsent=3", w.Founder)).ShouldBe(HttpStatusCode.OK);
        ExportResponse withFilters = await ExportAsync(client, w.Founder, "attendance", $"startDate=2026-11-01&endDate=2026-11-20&classroomId={w.School.Class.ClassroomId}&minAbsent=3");
        withFilters.Status.ShouldBe(HttpStatusCode.OK);
        List<AuditEntry> all = await ExportAuditAsync(factory, w.TenantId);
        all.Count.ShouldBe(3, "kabul edilen üç dışa aktarma; reddedilen yok");
        all.Select(a => a.Detail).ShouldContain($"report=Attendance;range=2026-11-01..2026-11-20;classroom={w.School.Class.ClassroomId};minAbsent=3;rows=0");
    }

    /// <summary>Tahsilatı doğrudan veritabanına yazar (10.000 satırı API'den girmek dakikalar sürer); makbuz no tekil dizini gereği sıralı.</summary>
    internal static async Task SeedPaymentsAsync(OkulApiFactory factory, IReadOnlyList<Guid> planIds, Guid actorId, long firstReceipt, int count)
    {
        await WithContextAsync(factory, async context =>
        {
            context.ChangeTracker.AutoDetectChangesEnabled = false;
            List<InstallmentPlan> plans = await context.InstallmentPlans.IgnoreQueryFilters().AsNoTracking().Where(p => planIds.Contains(p.Id)).ToListAsync();
            Dictionary<Guid, InstallmentPlan> byId = plans.ToDictionary(p => p.Id);
            DateOnly day = new(2026, 11, 20);
            for (int i = 0; i < count; i++)
            {
                InstallmentPlan plan = byId[planIds[i % planIds.Count]];
                context.Payments.Add(Payment.Record(plan, 1m, day.AddDays(-(i % 19)), (PaymentMethod)(1 + (i % 3)), null, firstReceipt + i, day, Morning, actorId));
            }
        });
    }
}
