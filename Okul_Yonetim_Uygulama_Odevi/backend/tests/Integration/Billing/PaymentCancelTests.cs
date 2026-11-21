using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Billing.BillingTestData;

namespace OkulYonetim.Tests.Integration.Billing;

/// <summary>
/// Spec 0015 AC-7 (tahsilat iptali): gerekçeyle iptal edilen tahsilatın dağılımı geri alınır ve taksit durumları yeniden hesaplanır
/// (geçmiş vadeli taksit yeniden "Gecikti"); gerekçesiz ve 201 karakterlik gerekçe alan hatası; aynı tahsilat ikinci kez iptal
/// edilemez; iptal edilen tahsilat cari toplamlarına ve hareket dökümüne girmez ama listede ve makbuzda "İPTAL EDİLDİ" (tarih,
/// gerekçe) olarak kalır.
/// </summary>
[Trait("Category", "Integration")]
public sealed class PaymentCancelTests
{
    private readonly TestDatabase database;

    public PaymentCancelTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Cancel_RevertsAllocation_PastDueOverdueAgain_StaysListedAsCancelled()
    {
        FakeTimeProvider time = new(Attendance.AttendanceTestData.Morning);
        using OkulApiFactory factory = BillingFactory(this.database, time: time);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        Guid fee = Id(await CreateTariffAsync(client, w.Founder, "Kurs", 3_000m));
        Guid plan = await PlanAsync(client, w.Founder, PlanBody(w.Ali, fee, firstDueDate: Today));
        (Guid keep, _) = await PaidAsync(client, w.Founder, plan, 200m);
        (Guid payment, long receipt) = await PaidAsync(client, w.Founder, plan, 800m);

        time.Advance(TimeSpan.FromDays(2));
        string founder = await FounderTokenAsync(client, w);
        States((await AccountAsync(client, founder, w.Ali)).Body.GetProperty("plans")[0]).ShouldBe(["Paid", "Pending", "Pending"]);

        foreach ((string? reason, string message) in new (string?, string)[] { (null, "İptal gerekçesi zorunludur"), ("   ", "İptal gerekçesi zorunludur"), (new string('g', 201), "İptal gerekçesi en fazla 200 karakter olabilir") })
        {
            var (status, problem, raw) = await CancelPaymentAsync(client, founder, payment, reason);
            status.ShouldBe(HttpStatusCode.BadRequest, raw);
            Detail(problem).ShouldBe(message, raw);
        }

        (await CancelPaymentAsync(client, founder, payment, "Tutar yanlış girildi")).Status.ShouldBe(HttpStatusCode.OK);
        var (again, againProblem, _) = await CancelPaymentAsync(client, founder, payment, "ikinci kez");
        again.ShouldBe(HttpStatusCode.BadRequest);
        Detail(againProblem).ShouldBe("Bu tahsilat zaten iptal edilmiş.");

        JsonElement account = (await AccountAsync(client, founder, w.Ali)).Body;
        JsonElement planView = account.GetProperty("plans")[0];
        States(planView).ShouldBe(["Overdue", "Pending", "Pending"]);
        Amounts(planView.GetProperty("installments"), "paid").ShouldBe([200m, 0m, 0m]);
        Money(account.GetProperty("totals"), "paid").ShouldBe(200m);
        Money(account.GetProperty("totals"), "overdue").ShouldBe(800m);
        JsonElement cancelled = account.GetProperty("payments").EnumerateArray().Single(p => p.GetProperty("id").GetGuid() == payment);
        cancelled.GetProperty("cancelled").GetBoolean().ShouldBeTrue();
        cancelled.GetProperty("cancelReason").GetString().ShouldBe("Tutar yanlış girildi");
        cancelled.GetProperty("cancelledAt").GetDateTimeOffset().ShouldBe(time.GetUtcNow());
        account.GetProperty("payments").EnumerateArray().Single(p => p.GetProperty("id").GetGuid() == keep).GetProperty("cancelled").GetBoolean().ShouldBeFalse();
        account.GetProperty("movements").EnumerateArray().Where(m => m.GetProperty("kind").GetString() == "Payment").Select(m => m.GetProperty("credit").GetDecimal()).ShouldBe([200m]);

        JsonElement slip = (await ReceiptAsync(client, founder, payment)).Body;
        slip.GetProperty("cancelled").GetBoolean().ShouldBeTrue();
        slip.GetProperty("cancelReason").GetString().ShouldBe("Tutar yanlış girildi");
        slip.GetProperty("receiptNo").GetInt64().ShouldBe(receipt);
        Money(slip, "amount").ShouldBe(800m);
    }
}
