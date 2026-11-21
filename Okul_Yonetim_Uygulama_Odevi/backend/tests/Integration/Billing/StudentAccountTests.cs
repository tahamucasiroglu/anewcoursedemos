using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Billing.BillingTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Billing;

/// <summary>
/// Spec 0015 AC-9 (cari ve gecikme): öğrenci carisinde borç, geçerli tahsilat toplamı, kalan ve geciken tutar doğru, hareket dökümü
/// tarih sırasıyla ve yürüyen bakiyeyle; vadesi bugün olan taksit gecikmiş sayılmaz, Türkiye saatiyle ertesi gün 00:00'dan sonra sayılır
/// (UTC'de hâlâ önceki gün olan saatte de); personel listesinde şube süzgeci, "yalnızca gecikenler", ad araması (Türkçe harf), 25'lik
/// sayfalama ve alt toplamlar doğru; liste boşken öğe yok (web "Henüz ödeme planı olan öğrenci yok").
/// </summary>
[Trait("Category", "Integration")]
public sealed class StudentAccountTests
{
    private readonly TestDatabase database;

    public StudentAccountTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Account_TotalsAndMovementsInDateOrder()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        Guid fee = Id(await CreateTariffAsync(client, w.Founder, "Kurs", 3_000m));
        Guid plan = await PlanAsync(client, w.Founder, PlanBody(w.Ali, fee, firstDueDate: Today));
        Guid lunch = Id(await CreateTariffAsync(client, w.Founder, "Yemek", 1_200m));
        Guid lunchPlan = await PlanAsync(client, w.Founder, PlanBody(w.Ali, lunch, installmentCount: 1, firstDueDate: "2026-12-15"));
        await PaidAsync(client, w.Founder, plan, 500m, date: "2026-11-10");
        await PaidAsync(client, w.Founder, lunchPlan, 200m, date: Today);
        (Guid cancelled, _) = await PaidAsync(client, w.Founder, plan, 100m, date: Today);
        (await CancelPaymentAsync(client, w.Founder, cancelled)).Status.ShouldBe(HttpStatusCode.OK);

        JsonElement account = (await AccountAsync(client, w.Founder, w.Ali)).Body;
        JsonElement totals = account.GetProperty("totals");
        Money(totals, "net").ShouldBe(4_200m);
        Money(totals, "paid").ShouldBe(700m);
        Money(totals, "remaining").ShouldBe(3_500m);
        Money(totals, "overdue").ShouldBe(0m, "vadesi bugün olan taksit gecikmiş değil");
        account.GetProperty("plans").GetArrayLength().ShouldBe(2);
        account.GetProperty("payments").GetArrayLength().ShouldBe(3);

        JsonElement[] movements = [.. account.GetProperty("movements").EnumerateArray()];
        movements.Select(m => m.GetProperty("date").GetString()).ShouldBe(["2026-11-10", Today, Today, Today]);
        movements.Select(m => m.GetProperty("kind").GetString()).ShouldBe(["Payment", "Plan", "Plan", "Payment"]);
        movements.Select(m => m.GetProperty("balance").GetDecimal()).ShouldBe([-500m, 2_500m, 3_700m, 3_500m]);
    }

    [Fact]
    public async Task Overdue_CountsFromTurkeyMidnight_EvenWhileUtcIsStillPreviousDay()
    {
        FakeTimeProvider time = new(Attendance.AttendanceTestData.Morning);
        using OkulApiFactory factory = BillingFactory(this.database, time: time);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        Guid fee = Id(await CreateTariffAsync(client, w.Founder, "Kurs", 3_000m));
        await PlanAsync(client, w.Founder, PlanBody(w.Ali, fee, firstDueDate: Today));

        time.SetUtcNow(new DateTimeOffset(2026, 11, 20, 20, 59, 0, TimeSpan.Zero));
        string founder = await FounderTokenAsync(client, w);
        JsonElement before = (await AccountAsync(client, founder, w.Ali)).Body;
        Money(before.GetProperty("totals"), "overdue").ShouldBe(0m, "Türkiye 23:59 — vade günü sürüyor");
        States(before.GetProperty("plans")[0])[0].ShouldBe("Pending");

        time.SetUtcNow(new DateTimeOffset(2026, 11, 20, 21, 1, 0, TimeSpan.Zero));
        JsonElement after = (await AccountAsync(client, founder, w.Ali)).Body;
        Money(after.GetProperty("totals"), "overdue").ShouldBe(1_000m, "Türkiye 00:01 — UTC hâlâ 20 Kasım");
        States(after.GetProperty("plans")[0])[0].ShouldBe("Overdue");
        Money((await StudentsAsync(client, founder, "?overdueOnly=true")).Body.GetProperty("totals"), "overdue").ShouldBe(1_000m);
    }

    [Fact]
    public async Task List_BranchFilterOverdueOnlySearchPagingAndTotals()
    {
        FakeTimeProvider time = new(Attendance.AttendanceTestData.Morning);
        using OkulApiFactory factory = BillingFactory(this.database, time: time);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        JsonElement empty = (await StudentsAsync(client, w.Founder)).Body;
        empty.GetProperty("items").GetArrayLength().ShouldBe(0);
        empty.GetProperty("totalCount").GetInt32().ShouldBe(0);

        Guid fee = Id(await CreateTariffAsync(client, w.Founder, "Kurs", 1_000m));
        await PlanAsync(client, w.Founder, PlanBody(w.Ali, fee, installmentCount: 1, firstDueDate: Today));
        await PlanAsync(client, w.Founder, PlanBody(w.Cem, fee, installmentCount: 1, firstDueDate: "2026-12-20"));
        for (int i = 1; i <= 25; i++)
        {
            Guid student = await CreateStudentAsync(client, w.Founder, NewStudent(w.BranchId, $"Öğrenci {i:D2}"));
            await PlanAsync(client, w.Founder, PlanBody(student, fee, installmentCount: 1, firstDueDate: "2026-12-20"));
        }

        time.Advance(TimeSpan.FromDays(1));
        string founder = await FounderTokenAsync(client, w);
        JsonElement first = (await StudentsAsync(client, founder)).Body;
        first.GetProperty("totalCount").GetInt32().ShouldBe(27);
        first.GetProperty("pageSize").GetInt32().ShouldBe(25);
        first.GetProperty("items").GetArrayLength().ShouldBe(25);
        Names(first)[0].ShouldBe("Ali Yılmaz");
        Money(first.GetProperty("totals"), "net").ShouldBe(27_000m);
        Money(first.GetProperty("totals"), "overdue").ShouldBe(1_000m);
        JsonElement second = (await StudentsAsync(client, founder, "?page=2")).Body;
        second.GetProperty("items").GetArrayLength().ShouldBe(2);
        Money(second.GetProperty("totals"), "net").ShouldBe(27_000m, "toplamlar sayfadan bağımsız, süzülmüş bütün satırlar");

        JsonElement overdue = (await StudentsAsync(client, founder, "?overdueOnly=true")).Body;
        Names(overdue).ShouldBe(["Ali Yılmaz"]);
        Money(overdue.GetProperty("totals"), "remaining").ShouldBe(1_000m);
        Names((await StudentsAsync(client, founder, $"?branchId={w.OtherBranchId}")).Body).ShouldBe(["Cem Arslan"]);
        Names((await StudentsAsync(client, founder, "?search=YILMAZ")).Body).ShouldBe(["Ali Yılmaz"]);
        Names((await StudentsAsync(client, founder, "?search=%C3%B6%C4%9Frenci%2025")).Body).ShouldBe(["Öğrenci 25"]);
        (await StudentsAsync(client, founder, "?page=0")).Status.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static string[] Names(JsonElement page) => [.. page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("fullName").GetString()!)];
}
