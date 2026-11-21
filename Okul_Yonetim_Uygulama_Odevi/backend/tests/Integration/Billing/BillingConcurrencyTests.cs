using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OkulYonetim.Application.Ports;
using OkulYonetim.Persistence.Repositories;
using OkulYonetim.Tests.Integration.Administration;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Billing.BillingTestData;

namespace OkulYonetim.Tests.Integration.Billing;

/// <summary>
/// Spec 0015 AC-12 (eşzamanlılık, belirlenimci — <see cref="RaceGate"/>): aynı plana aynı anda gönderilen iki tahsilat toplamda kalan
/// borcu aşamaz — biri "Tutar kalan borçtan fazla olamaz." ile reddedilir, taksit fazla ödenmez; iki farklı plana aynı anda gelen
/// tahsilatlar aynı makbuz numarasını almaz (1 ve 2). İlk istek sıra içindeki okumada bekletilir; ikinci istek sırayı beklemeden
/// okuyamamalıdır (sıra kaldırılınca kapıya girer ve test kırmızı olur).
/// </summary>
[Trait("Category", "Integration")]
public sealed class BillingConcurrencyTests
{
    private readonly TestDatabase database;

    public BillingConcurrencyTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task SamePlan_TwoPaymentsAtOnce_RemainingNeverExceeded()
    {
        RaceGate installments = new();
        using OkulApiFactory factory = GatedFactory(installments, new RaceGate());
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        Guid fee = Id(await CreateTariffAsync(client, w.Founder, "Kurs", 1_000m));
        Guid plan = await PlanAsync(client, w.Founder, PlanBody(w.Ali, fee, installmentCount: 1));

        installments.Enabled = true;
        var first = PayAsync(client, w.Founder, PaymentBody(plan, 700m));
        await installments.FirstEntered.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var second = PayAsync(client, w.Accountant, PaymentBody(plan, 700m));
        var results = await Task.WhenAll(first, second);
        installments.Enabled = false;

        installments.SecondEnteredWhileFirstWaited.ShouldBe(false, "ikinci tahsilat sırayı beklemeden kalanı okudu");
        results.Count(r => r.Status == HttpStatusCode.OK).ShouldBe(1);
        var rejected = results.Single(r => r.Status != HttpStatusCode.OK);
        rejected.Status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(rejected.Body).ShouldBe("Tutar kalan borçtan fazla olamaz.");
        var totals = (await AccountAsync(client, w.Founder, w.Ali)).Body.GetProperty("totals");
        Money(totals, "paid").ShouldBe(700m);
        Money(totals, "remaining").ShouldBe(300m);
        (await PaymentCountAsync(factory, w.TenantId)).ShouldBe(1);
    }

    [Fact]
    public async Task TwoPlans_TwoPaymentsAtOnce_DistinctReceiptNumbers()
    {
        RaceGate counter = new();
        using OkulApiFactory factory = GatedFactory(new RaceGate(), counter);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        Guid ali = await PlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId));
        Guid ayse = await PlanAsync(client, w.Founder, PlanBody(w.School.Ayse, w.FeeId));

        counter.Enabled = true;
        var first = PayAsync(client, w.Founder, PaymentBody(ali, 100m));
        await counter.FirstEntered.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var second = PayAsync(client, w.Accountant, PaymentBody(ayse, 100m));
        var results = await Task.WhenAll(first, second);
        counter.Enabled = false;

        counter.SecondEnteredWhileFirstWaited.ShouldBe(false, "ikinci tahsilat sayaç sırasını beklemeden numara aldı");
        results.ShouldAllBe(r => r.Status == HttpStatusCode.OK);
        results.Select(r => r.Body.GetProperty("receiptNo").GetInt64()).ShouldBe([1L, 2L], ignoreOrder: true);
        (await PaidAsync(client, w.Founder, ali, 100m)).ReceiptNo.ShouldBe(3);
    }

    private OkulApiFactory GatedFactory(RaceGate installments, RaceGate counter) =>
        new(this.database.ConnectionString, timeProvider: new Microsoft.Extensions.Time.Testing.FakeTimeProvider(Attendance.AttendanceTestData.Morning), configureServices: services =>
        {
            services.RemoveAll<IEmailOutbox>();
            services.AddSingleton<IEmailOutbox>(new Account.CapturingOutbox());
            services.RemoveAll<IBillingRepository>();
            services.AddScoped<BillingRepository>();
            services.AddScoped<IBillingRepository>(sp => new GatedBillingRepository(sp.GetRequiredService<BillingRepository>(), installments, counter));
        });
}
