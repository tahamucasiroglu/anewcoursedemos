using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Billing;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Billing.BillingTestData;
using static OkulYonetim.Tests.Integration.Dashboard.DashboardTestData;

namespace OkulYonetim.Tests.Integration.Dashboard;

/// <summary>
/// Spec 0017 AC-8 (tahsilat raporu; S4) ve AC-6'nın sunucu tarafı (sayfa, toplam, boş sonuç, alan hataları): aralıktaki tahsilatlar (sınır
/// günleri dahil, dışındakiler yok) tarih ve makbuz no sırasıyla listelenir; iptal tahsilat "iptal" işaretli görünür ve toplamlara girmez;
/// geçerli toplam ve yönteme göre toplamlar doğru; 50'lik sayfa ve toplamlar bütün sonuç üzerinden; eksik tarih, ters ve 367 günlük aralık
/// Türkçe alan hatasıyla 400, 366 gün kabul.
/// </summary>
[Trait("Category", "Integration")]
public sealed class PaymentsReportTests
{
    private readonly TestDatabase database;

    public PaymentsReportTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task RangeBoundaries_CancelledMarkedAndExcluded_MethodTotals()
    {
        using OkulApiFactory factory = DashboardFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await DashboardWorldAsync(factory, client);
        Guid plan = await PlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId));
        await PaidAsync(client, w.Founder, plan, 100m, "2026-10-31");
        await PaidAsync(client, w.Founder, plan, 200m, "2026-11-01");
        (Guid cancelled, _) = await PaidAsync(client, w.Founder, plan, 999m, "2026-11-10");
        (await CancelPaymentAsync(client, w.Founder, cancelled)).Status.ShouldBe(HttpStatusCode.OK);
        await PaidAsync(client, w.Founder, plan, 300.50m, "2026-11-15", "BankTransfer");
        await PaidAsync(client, w.Founder, plan, 400.25m, "2026-11-20", "CardPos");

        var (status, report, raw) = await PaymentsReportAsync(client, w.Founder, "startDate=2026-11-01&endDate=2026-11-20");

        status.ShouldBe(HttpStatusCode.OK, raw);
        JsonElement items = report.GetProperty("items");
        items.EnumerateArray().Select(i => i.GetProperty("receiptNo").GetInt64()).ShouldBe([2L, 3L, 4L, 5L]);
        items.EnumerateArray().Select(i => i.GetProperty("date").GetString()).ShouldBe(["2026-11-01", "2026-11-10", "2026-11-15", "2026-11-20"]);
        items.EnumerateArray().Select(i => i.GetProperty("cancelled").GetBoolean()).ShouldBe([false, true, false, false]);
        items.EnumerateArray().Select(i => i.GetProperty("method").GetString()).ShouldBe(["Cash", "Cash", "BankTransfer", "CardPos"]);
        items[0].GetProperty("studentName").GetString().ShouldBe("Ali Yılmaz");
        items[0].GetProperty("branchName").GetString().ShouldBe("Merkez");
        items[0].GetProperty("recordedByName").GetString().ShouldBe("Ayşe Kurucu");
        JsonElement totals = report.GetProperty("totals");
        Money(totals, "validAmount").ShouldBe(900.75m);
        totals.GetProperty("validCount").GetInt32().ShouldBe(3);
        totals.GetProperty("cancelledCount").GetInt32().ShouldBe(1);
        totals.GetProperty("byMethod").EnumerateArray().Select(m => (m.GetProperty("method").GetString(), m.GetProperty("amount").GetDecimal()))
            .ShouldBe([("Cash", 200m), ("BankTransfer", 300.50m), ("CardPos", 400.25m)]);
        raw.ShouldNotContain("Yanlış öğrenciye girildi", Case.Sensitive);
    }

    [Fact]
    public async Task FiftyPerPage_TotalsOverWholeResult_EmptyResult_AndFilterErrors()
    {
        using OkulApiFactory factory = DashboardFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await DashboardWorldAsync(factory, client);
        Guid plan = await PlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId, installmentCount: 1));
        for (int i = 0; i < 55; i++)
        {
            await PaidAsync(client, w.Founder, plan, 10m);
        }

        JsonElement first = (await PaymentsReportAsync(client, w.Founder, "startDate=2026-11-01&endDate=2026-11-20")).Body;
        JsonElement second = (await PaymentsReportAsync(client, w.Founder, "startDate=2026-11-01&endDate=2026-11-20&page=2")).Body;
        first.GetProperty("items").GetArrayLength().ShouldBe(50);
        first.GetProperty("pageSize").GetInt32().ShouldBe(50);
        second.GetProperty("items").GetArrayLength().ShouldBe(5);
        foreach (JsonElement page in new[] { first, second })
        {
            page.GetProperty("totalCount").GetInt32().ShouldBe(55);
            Money(page.GetProperty("totals"), "validAmount").ShouldBe(550m);
        }

        second.GetProperty("items")[4].GetProperty("receiptNo").GetInt64().ShouldBe(55);
        (await PaymentsReportAsync(client, w.Founder, "startDate=2026-10-01&endDate=2026-10-31")).Body.GetProperty("totalCount").GetInt32().ShouldBe(0);

        foreach ((string query, string message) in new[]
        {
            ("endDate=2026-11-20", "Başlangıç tarihi zorunludur"),
            ("startDate=2026-11-20", "Bitiş tarihi zorunludur"),
            ("startDate=2026-11-21&endDate=2026-11-20", "Başlangıç tarihi bitiş tarihinden sonra olamaz"),
            ("startDate=2025-11-19&endDate=2026-11-20", "Tarih aralığı en fazla 366 gün olabilir"),
        })
        {
            var (status, problem, _) = await PaymentsReportAsync(client, w.Founder, query);
            status.ShouldBe(HttpStatusCode.BadRequest, query);
            Administration.AdminTestData.Detail(problem).ShouldBe(message, query);
        }

        (await PaymentsReportAsync(client, w.Founder, "startDate=2025-11-20&endDate=2026-11-20")).Status.ShouldBe(HttpStatusCode.OK, "366 gün (uçlar dahil) kabul");
    }
}
