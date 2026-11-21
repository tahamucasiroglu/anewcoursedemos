using System.Net;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Billing.BillingTestData;

namespace OkulYonetim.Tests.Integration.Billing;

/// <summary>
/// Spec 0015 AC-12 (gizlilik): tarife/plan/tahsilat/iptal/hatırlatma boyunca log satırlarında öğrenci ve veli adı, tahsilat notu, iptal
/// gerekçesi, tarife adı ve hatırlatma metni geçmez (yalnızca kimlikler); hata yanıtlarında (iş kuralı ihlali, doğrulama) da öğrenci adı,
/// not ve gerekçe yok. Hatırlatma gönderimi Information log'unda kimliklerle görünür.
/// </summary>
[Trait("Category", "Integration")]
public sealed class BillingSecretHygieneTests
{
    private readonly TestDatabase database;

    public BillingSecretHygieneTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task LogsAndErrorResponses_CarryNoNamesNotesOrReasons()
    {
        FakeTimeProvider time = new(Attendance.AttendanceTestData.Morning);
        using OkulApiFactory factory = BillingFactory(this.database, new CapturingPushNotifier(), time);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        Guid fee = Id(await CreateTariffAsync(client, w.Founder, "Gizli Tarife Adı", 1_000m));
        Guid plan = await PlanAsync(client, w.Accountant, PlanBody(w.Ali, fee, installmentCount: 2, firstDueDate: Today));
        (Guid payment, _) = await PaidAsync(client, w.Accountant, plan, 100m, note: "NOT-SIRRI-111");
        (await CancelPaymentAsync(client, w.Accountant, payment, "GEREKCE-SIRRI-222")).Status.ShouldBe(HttpStatusCode.OK);

        List<string> errors = [];
        foreach (var call in new[]
        {
            await PayAsync(client, w.Accountant, PaymentBody(plan, 5_000m, note: "NOT-SIRRI-111")),
            await PayAsync(client, w.Accountant, PaymentBody(plan, 1m, note: new string('x', 201))),
            await CancelPaymentAsync(client, w.Accountant, payment, "GEREKCE-SIRRI-222"),
            await CancelPlanAsync(client, w.Accountant, Guid.NewGuid()),
            await CreatePlanAsync(client, w.Accountant, PlanBody(w.Ali, fee)),
        })
        {
            call.Status.ShouldBe(call.Status == HttpStatusCode.NotFound ? HttpStatusCode.NotFound : HttpStatusCode.BadRequest, call.Raw);
            errors.Add(call.Raw);
        }

        time.SetUtcNow(new DateTimeOffset(2026, 11, 21, 8, 0, 0, TimeSpan.Zero));
        (await RunRemindersAsync(factory, w.TenantId)).ShouldBe(1);

        string log = factory.ReadLog();
        log.ShouldContain("Tahsilat kaydedildi");
        log.ShouldContain("Gecikme hatırlatması gönderildi");
        log.ShouldContain(w.Ali.ToString());
        string[] secrets = ["Ali Yılmaz", "Ayşe Kaya", "NOT-SIRRI-111", "GEREKCE-SIRRI-222", "Gizli Tarife Adı", "ödenmemiştir", "Veli Kişi"];
        foreach (string secret in secrets)
        {
            log.ShouldNotContain(secret, customMessage: secret);
        }

        foreach (string body in errors)
        {
            foreach (string secret in new[] { "Ali Yılmaz", "NOT-SIRRI-111", "GEREKCE-SIRRI-222" })
            {
                body.ShouldNotContain(secret, customMessage: secret);
            }
        }
    }
}
