using System.Net;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Billing.BillingTestData;

namespace OkulYonetim.Tests.Integration.Billing;

/// <summary>
/// Spec 0015 AC-1 (yetki): öğretmen bütün tahsilat işlemlerinde 403 ve hiçbir şey değişmez; muhasebe tarife/indirim ekleme-düzenleme-
/// pasifleştirme-etkinleştirmede 403 (okuyabilir — plan formu); veli personel uçlarında 403, öğretmen ve muhasebe veli ucunda 403;
/// girişsiz istek 401; rolü alınan muhasebenin bir sonraki isteği reddedilir (rol her istekte veritabanından).
/// </summary>
[Trait("Category", "Integration")]
public sealed class BillingAuthorizationTests
{
    private readonly TestDatabase database;

    public BillingAuthorizationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task TeacherAndGuardian_403_OnEveryStaffEndpoint_NothingChanges()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        Guid plan = await PlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId));
        (Guid payment, _) = await PaidAsync(client, w.Founder, plan, 100m);

        foreach (string token in new[] { w.School.Class.Teacher, w.School.AliMother.Token })
        {
            (await TariffsAsync(client, token)).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await CreateTariffAsync(client, token, "Servis", 100m)).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await UpdateTariffAsync(client, token, w.FeeId, "X", 1m)).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await SetTariffActiveAsync(client, token, w.FeeId, false)).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await DiscountsAsync(client, token)).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await CreateDiscountAsync(client, token, "Y", 5)).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await StudentsAsync(client, token)).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await AccountAsync(client, token, w.Ali)).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await PreviewAsync(client, token, PlanBody(w.School.Ayse, w.FeeId))).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await CreatePlanAsync(client, token, PlanBody(w.School.Ayse, w.FeeId))).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await CancelPlanAsync(client, token, plan)).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await PayAsync(client, token, PaymentBody(plan, 50m))).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await CancelPaymentAsync(client, token, payment)).Status.ShouldBe(HttpStatusCode.Forbidden);
            (await ReceiptAsync(client, token, payment)).Status.ShouldBe(HttpStatusCode.Forbidden);
        }

        (await MyPaymentsAsync(client, w.School.Class.Teacher)).Status.ShouldBe(HttpStatusCode.Forbidden, "veli rolü yok");
        (await MyPaymentsAsync(client, w.Accountant)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await MyPaymentsAsync(client, w.School.AliMother.Token)).Status.ShouldBe(HttpStatusCode.OK);

        (await PlanCountAsync(factory, w.School.Ayse)).ShouldBe(0);
        (await PaymentCountAsync(factory, w.TenantId)).ShouldBe(1);
        (await FeeRowsAsync(factory, w.TenantId)).ShouldHaveSingleItem().IsActive.ShouldBeTrue();
        (await DiscountRowsAsync(factory, w.TenantId)).Count.ShouldBe(1);
        Money((await AccountAsync(client, w.Founder, w.Ali)).Body.GetProperty("totals"), "paid").ShouldBe(100m);

        foreach (string path in new[] { "/api/billing/tariffs", "/api/billing/students", $"/api/billing/students/{w.Ali}", $"/api/billing/payments/{payment}/receipt", "/api/my/payments" })
        {
            using HttpResponseMessage anonymous = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
            anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task Accountant_ReadsCatalog_ButCatalogWrites403_NothingChanges()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);

        (await TariffsAsync(client, w.Accountant)).Status.ShouldBe(HttpStatusCode.OK);
        (await DiscountsAsync(client, w.Accountant)).Status.ShouldBe(HttpStatusCode.OK);
        var (create, problem, _) = await CreateTariffAsync(client, w.Accountant, "Servis", 100m);
        create.ShouldBe(HttpStatusCode.Forbidden);
        Detail(problem).ShouldBe("Bu işlemi yalnızca kurucu yapabilir.");
        (await UpdateTariffAsync(client, w.Accountant, w.FeeId, "Değişti", 1m)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await SetTariffActiveAsync(client, w.Accountant, w.FeeId, false)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await SetTariffActiveAsync(client, w.Accountant, w.FeeId, true)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await CreateDiscountAsync(client, w.Accountant, "Personel", 5)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await UpdateDiscountAsync(client, w.Accountant, w.DiscountId, "Değişti", 50)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await SetDiscountActiveAsync(client, w.Accountant, w.DiscountId, false)).Status.ShouldBe(HttpStatusCode.Forbidden);

        var fee = (await FeeRowsAsync(factory, w.TenantId)).ShouldHaveSingleItem();
        fee.Name.ShouldBe("2026-2027 Eğitim Ücreti");
        fee.Amount.ShouldBe(10_000m);
        fee.IsActive.ShouldBeTrue();
        var discount = (await DiscountRowsAsync(factory, w.TenantId)).ShouldHaveSingleItem();
        discount.Percent.ShouldBe(15);
        discount.IsActive.ShouldBeTrue();
        (await BillingAuditAsync(factory, w.TenantId)).Count.ShouldBe(2, "yalnızca kurucunun iki eklemesi");
    }

    [Fact]
    public async Task AccountantRoleRevoked_NextRequestRejected()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        Guid plan = await PlanAsync(client, w.Accountant, PlanBody(w.Ali, w.FeeId));
        (await StudentsAsync(client, w.Accountant)).Status.ShouldBe(HttpStatusCode.OK);

        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/members/{w.AccountantId}/roles", w.Founder, new { roles = new[] { new { role = "Teacher", branchIds = new[] { w.BranchId } } } }))
            .ShouldBe(HttpStatusCode.OK);

        (await StudentsAsync(client, w.Accountant)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await PayAsync(client, w.Accountant, PaymentBody(plan, 100m))).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await CancelPlanAsync(client, w.Accountant, plan)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await PaymentCountAsync(factory, w.TenantId)).ShouldBe(0);
    }
}
