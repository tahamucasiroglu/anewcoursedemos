using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Tests.Integration.Billing;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Billing.BillingTestData;
using static OkulYonetim.Tests.Integration.Dashboard.DashboardTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Dashboard;

/// <summary>
/// Spec 0017 AC-4 (geciken ödemeler kartı; S4): gecikmeli öğrenci sayısı ve toplam geciken tutar 0015 cari listesinin ("yalnızca gecikenler")
/// sayısına ve geciken toplamına eşittir; iptal plan ve iptal tahsilat hesabı bozmaz, kısmi tahsilat düşülür; vadesi bugün olan taksit
/// sayılmaz; liste en eski vadeden ilk 10, gün sayısı doğru; gecikme yokken öğrenci sayısı 0 ve liste boş.
/// </summary>
[Trait("Category", "Integration")]
public sealed class OverdueCardTests
{
    private readonly TestDatabase database;

    public OverdueCardTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task OverdueTotals_EqualLedger_CancelledIgnored_DueTodayNotOverdue()
    {
        FakeTimeProvider clock = Clock();
        using OkulApiFactory factory = DashboardFactory(this.database, clock);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await DashboardWorldAsync(factory, client);
        Guid ali = await PlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId, installmentCount: 3, firstDueDate: "2026-12-01"));
        await PaidAsync(client, w.Founder, ali, 1_000m);
        Guid ayse = await PlanAsync(client, w.Founder, PlanBody(w.School.Ayse, w.FeeId, installmentCount: 1, firstDueDate: "2026-12-01"));
        (await CancelPlanAsync(client, w.Founder, ayse)).Status.ShouldBe(HttpStatusCode.OK);
        Guid berk = await PlanAsync(client, w.Founder, PlanBody(w.School.Berk, w.FeeId, w.DiscountId, installmentCount: 2, firstDueDate: "2026-12-15"));
        (Guid berkPayment, _) = await PaidAsync(client, w.Founder, berk, 1_000m);
        (await CancelPaymentAsync(client, w.Founder, berkPayment)).Status.ShouldBe(HttpStatusCode.OK);
        await PlanAsync(client, w.Founder, PlanBody(w.Cem, w.FeeId, installmentCount: 1, firstDueDate: "2027-01-01"));

        JsonElement before = (await OverdueCardAsync(client, w.Founder)).Body;
        before.GetProperty("studentCount").GetInt32().ShouldBe(0, "henüz vadesi geçen yok");
        before.GetProperty("top").GetArrayLength().ShouldBe(0);

        clock.SetUtcNow(new DateTimeOffset(2027, 1, 1, 9, 0, 0, TimeSpan.Zero));
        string founder = await FounderTokenAsync(client, w);
        var (status, card, raw) = await OverdueCardAsync(client, founder);

        status.ShouldBe(HttpStatusCode.OK, raw);
        card.GetProperty("today").GetString().ShouldBe("2027-01-01");
        card.GetProperty("studentCount").GetInt32().ShouldBe(2, "Ali ve Berk; Ayşe'nin planı iptal, Cem'in vadesi bugün");
        Money(card, "overdueAmount").ShouldBe(2_333.33m + 4_250.00m);
        JsonElement top = card.GetProperty("top");
        Names(top).ShouldBe(["Ali Yılmaz", "Berk Demir"]);
        top[0].GetProperty("oldestDue").GetString().ShouldBe("2026-12-01");
        top[0].GetProperty("daysOverdue").GetInt32().ShouldBe(31);
        Money(top[0], "overdueAmount").ShouldBe(2_333.33m);
        top[1].GetProperty("daysOverdue").GetInt32().ShouldBe(17);
        top[1].GetProperty("classroomName").GetString().ShouldBe("5-B");

        JsonElement ledger = (await StudentsAsync(client, founder, "?overdueOnly=true")).Body;
        card.GetProperty("studentCount").GetInt32().ShouldBe(ledger.GetProperty("totalCount").GetInt32());
        Money(card, "overdueAmount").ShouldBe(Money(ledger.GetProperty("totals"), "overdue"));
    }

    [Fact]
    public async Task MoreThanTen_FirstTenByOldestDue()
    {
        FakeTimeProvider clock = Clock();
        using OkulApiFactory factory = DashboardFactory(this.database, clock);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await DashboardWorldAsync(factory, client);
        for (int i = 0; i < 11; i++)
        {
            Guid student = await CreateStudentAsync(client, w.Founder, NewStudent(w.BranchId, $"Öğrenci {10 - i:00}", classroomId: w.School.Class.ClassroomId));
            await PlanAsync(client, w.Founder, PlanBody(student, w.FeeId, installmentCount: 1, firstDueDate: $"2026-12-{1 + i:00}"));
        }

        clock.SetUtcNow(new DateTimeOffset(2027, 1, 1, 9, 0, 0, TimeSpan.Zero));
        JsonElement card = (await OverdueCardAsync(client, await FounderTokenAsync(client, w))).Body;

        card.GetProperty("studentCount").GetInt32().ShouldBe(11);
        Money(card, "overdueAmount").ShouldBe(110_000m);
        JsonElement top = card.GetProperty("top");
        top.GetArrayLength().ShouldBe(10);
        top.EnumerateArray().Select(t => t.GetProperty("oldestDue").GetString()).ShouldBe([.. Enumerable.Range(1, 10).Select(d => $"2026-12-{d:00}")]);
        top.EnumerateArray().Select(t => t.GetProperty("daysOverdue").GetInt32()).ShouldBe([.. Enumerable.Range(1, 10).Select(d => 32 - d)]);
        Names(top).ShouldNotContain("Öğrenci 00", "en yeni vade (11 Aralık) ilk 10'da değil");
    }
}
