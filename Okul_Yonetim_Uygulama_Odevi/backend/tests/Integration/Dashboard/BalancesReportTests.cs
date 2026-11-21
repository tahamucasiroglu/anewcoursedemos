using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Tests.Integration.Billing;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Billing.BillingTestData;
using static OkulYonetim.Tests.Integration.Dashboard.DashboardTestData;

namespace OkulYonetim.Tests.Integration.Dashboard;

/// <summary>
/// Spec 0017 AC-9 (borç/gecikme raporu; S4): öğrenci başına net, ödenen, kalan, geciken 0015 öğrenci carisiyle aynıdır, en eski geciken vade
/// bugün gecikmiş taksitlerin en eskisidir (iki planlı öğrencide de); iptal plan listede yok; "yalnızca gecikenler" süzgeci ve toplamlar doğru.
/// </summary>
[Trait("Category", "Integration")]
public sealed class BalancesReportTests
{
    private readonly TestDatabase database;

    public BalancesReportTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task RowsEqualLedger_CancelledPlanAbsent_OverdueOnlyAndTotals()
    {
        FakeTimeProvider clock = Clock();
        using OkulApiFactory factory = DashboardFactory(this.database, clock);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await DashboardWorldAsync(factory, client);
        Guid service = Id(await CreateTariffAsync(client, w.Founder, "Servis", 1_200m));
        Guid ali = await PlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId, installmentCount: 3, firstDueDate: "2026-12-01"));
        await PaidAsync(client, w.Founder, ali, 1_000m);
        await PlanAsync(client, w.Founder, PlanBody(w.Ali, service, installmentCount: 1, firstDueDate: "2026-12-10"));
        Guid ayse = await PlanAsync(client, w.Founder, PlanBody(w.School.Ayse, w.FeeId, installmentCount: 1, firstDueDate: "2026-12-01"));
        await CancelPlanAsync(client, w.Founder, ayse);
        await PlanAsync(client, w.Founder, PlanBody(w.School.Berk, w.FeeId, w.DiscountId, installmentCount: 2, firstDueDate: "2026-12-15"));
        await PlanAsync(client, w.Founder, PlanBody(w.Cem, w.FeeId, installmentCount: 1, firstDueDate: "2027-01-01"));
        clock.SetUtcNow(new DateTimeOffset(2027, 1, 1, 9, 0, 0, TimeSpan.Zero));
        string founder = await FounderTokenAsync(client, w);

        JsonElement report = (await BalancesReportAsync(client, founder)).Body;

        report.GetProperty("today").GetString().ShouldBe("2027-01-01");
        JsonElement items = report.GetProperty("items");
        Names(items).ShouldBe(["Ali Yılmaz", "Berk Demir", "Cem Arslan"]);
        foreach (JsonElement row in items.EnumerateArray())
        {
            JsonElement ledger = (await AccountAsync(client, founder, row.GetProperty("studentId").GetGuid())).Body.GetProperty("totals");
            foreach (string field in new[] { "net", "paid", "remaining", "overdue" })
            {
                Money(row, field).ShouldBe(Money(ledger, field), $"{row.GetProperty("fullName").GetString()} {field}");
            }
        }

        items[0].GetProperty("planNames").GetString().ShouldBe("2026-2027 Eğitim Ücreti, Servis");
        items[0].GetProperty("oldestOverdueDue").GetString().ShouldBe("2026-12-01");
        Money(items[0], "overdue").ShouldBe(2_333.33m + 1_200m);
        items[1].GetProperty("oldestOverdueDue").GetString().ShouldBe("2026-12-15");
        items[1].GetProperty("classroomName").GetString().ShouldBe("5-B");
        items[2].GetProperty("oldestOverdueDue").ValueKind.ShouldBe(JsonValueKind.Null, "vadesi bugün olan taksit gecikmiş değil");
        Money(items[2], "overdue").ShouldBe(0m);
        JsonElement totals = report.GetProperty("totals");
        Money(totals, "net").ShouldBe(10_000m + 1_200m + 8_500m + 10_000m);
        Money(totals, "paid").ShouldBe(1_000m);
        Money(totals, "remaining").ShouldBe(28_700m);
        Money(totals, "overdue").ShouldBe(3_533.33m + 4_250m);

        JsonElement overdue = (await BalancesReportAsync(client, founder, "overdueOnly=true")).Body;
        Names(overdue.GetProperty("items")).ShouldBe(["Ali Yılmaz", "Berk Demir"]);
        overdue.GetProperty("totalCount").GetInt32().ShouldBe(2);
        Money(overdue.GetProperty("totals"), "overdue").ShouldBe(7_783.33m);
        Money(overdue.GetProperty("totals"), "net").ShouldBe(19_700m);
    }
}
