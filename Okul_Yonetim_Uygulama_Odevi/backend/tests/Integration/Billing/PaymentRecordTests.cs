using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Billing.BillingTestData;

namespace OkulYonetim.Tests.Integration.Billing;

/// <summary>
/// Spec 0015 AC-6 (tahsilat ve dağıtım): 3 × 1.000 TL taksitli planda 1.500 TL tahsilat 1. taksidi "Ödendi", 2.'yi "Kısmen ödendi" (500
/// kalan) yapar; kalan borca eşit tutar kabul edilir, 0,01 fazlası "Tutar kalan borçtan fazla olamaz." ile reddedilir; tutar 0, 3 ondalık,
/// gelecek tarih, 366 gün önceki tarih, eksik/yanlış yöntem, 201 karakterlik not alan hatası (Türkçe) verir ve hiçbir şey kaydedilmez
/// (365 gün önce ve 200 karakter kabul); iptal planına tahsilat reddedilir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class PaymentRecordTests
{
    private readonly TestDatabase database;

    public PaymentRecordTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task FifteenHundredOnThreeThousands_FirstPaidSecondPartial_ExactRemainderAccepted_OneCentMoreRejected()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        Guid fee = Id(await CreateTariffAsync(client, w.Founder, "Kurs", 3_000m));
        Guid plan = await PlanAsync(client, w.Accountant, PlanBody(w.Ali, fee));

        (Guid id, long receipt) = await PaidAsync(client, w.Accountant, plan, 1_500m, method: "BankTransfer", note: "Dekont 42");
        receipt.ShouldBe(1);
        JsonElement account = (await AccountAsync(client, w.Accountant, w.Ali)).Body;
        JsonElement planView = account.GetProperty("plans")[0];
        States(planView).ShouldBe(["Paid", "PartiallyPaid", "Pending"]);
        Amounts(planView.GetProperty("installments"), "remaining").ShouldBe([0m, 500m, 1_000m]);
        Money(account.GetProperty("totals"), "paid").ShouldBe(1_500m);
        Money(account.GetProperty("totals"), "remaining").ShouldBe(1_500m);
        JsonElement row = account.GetProperty("payments")[0];
        row.GetProperty("id").GetGuid().ShouldBe(id);
        row.GetProperty("method").GetString().ShouldBe("BankTransfer");
        row.GetProperty("note").GetString().ShouldBe("Dekont 42");

        var (over, problem, _) = await PayAsync(client, w.Accountant, PaymentBody(plan, 1_500.01m));
        over.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Tutar kalan borçtan fazla olamaz.");
        (await PaymentCountAsync(factory, w.TenantId)).ShouldBe(1);

        await PaidAsync(client, w.Accountant, plan, 1_500.00m, method: "CardPos");
        JsonElement paid = (await AccountAsync(client, w.Accountant, w.Ali)).Body;
        States(paid.GetProperty("plans")[0]).ShouldBe(["Paid", "Paid", "Paid"]);
        Money(paid.GetProperty("totals"), "remaining").ShouldBe(0m);
        Detail((await PayAsync(client, w.Accountant, PaymentBody(plan, 0.01m))).Body).ShouldBe("Tutar kalan borçtan fazla olamaz.");
    }

    [Fact]
    public async Task InvalidFields_FieldErrors_NothingSaved_CancelledPlanRejected()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        Guid plan = await PlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId));

        foreach ((object body, string message) in new (object, string)[]
        {
            (PaymentBody(plan, 0m), "Tutar sıfırdan büyük olmalı"),
            (PaymentBody(plan, 10.005m), "Tutar en fazla 2 ondalık olabilir"),
            (PaymentBody(plan, "on lira"), "Tutar geçerli bir sayı olmalı"),
            (PaymentBody(plan, null), "Tutar zorunludur"),
            (PaymentBody(plan, 10m, date: "2026-11-21"), "Tarih gelecekte olamaz"),
            (PaymentBody(plan, 10m, date: "2025-11-19"), "Tarih en fazla 365 gün önce olabilir"),
            (PaymentBody(plan, 10m, date: null), "Tarih zorunludur"),
            (PaymentBody(plan, 10m, method: null), "Ödeme yöntemi seçin"),
            (PaymentBody(plan, 10m, method: "Cheque"), "Ödeme yöntemi seçin"),
            (PaymentBody(plan, 10m, method: "cash"), "Ödeme yöntemi seçin"),
            (PaymentBody(plan, 10m, note: new string('n', 201)), "Not en fazla 200 karakter olabilir"),
            (PaymentBody(Guid.Empty, 10m), "Plan zorunludur"),
        })
        {
            var (status, problem, raw) = await PayAsync(client, w.Founder, body);
            status.ShouldBe(HttpStatusCode.BadRequest, raw);
            Detail(problem).ShouldBe(message, raw);
        }

        (await PaymentCountAsync(factory, w.TenantId)).ShouldBe(0);
        Money((await AccountAsync(client, w.Founder, w.Ali)).Body.GetProperty("totals"), "paid").ShouldBe(0m);
        (await PaidAsync(client, w.Founder, plan, 10m, date: "2025-11-20", note: new string('n', 200))).ReceiptNo.ShouldBe(1, "365 gün önce ve 200 karakter sınırda kabul");

        Guid other = Id(await CreateTariffAsync(client, w.Founder, "Servis", 900m));
        Guid cancelled = await PlanAsync(client, w.Founder, PlanBody(w.Ali, other));
        (await CancelPlanAsync(client, w.Founder, cancelled)).Status.ShouldBe(HttpStatusCode.OK);
        Detail((await PayAsync(client, w.Founder, PaymentBody(cancelled, 100m))).Body).ShouldBe("İptal edilmiş plana tahsilat girilemez.");
        (await PaymentCountAsync(factory, w.TenantId)).ShouldBe(1);
    }
}
