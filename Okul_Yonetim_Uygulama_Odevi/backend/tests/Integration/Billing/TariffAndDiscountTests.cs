using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Billing.BillingTestData;

namespace OkulYonetim.Tests.Integration.Billing;

/// <summary>
/// Spec 0015 AC-3 (tarife ve indirim): geçerli tarife/indirim eklenir, düzenlenir, pasifleştirilir ve yeniden etkinleştirilir; boş ad,
/// 101 karakter ad, tutar 0 / negatif / 3 ondalık / 10.000.000,01 / sayı olmayan, yüzde 0 / 101 / ondalıklı alan hatası (Türkçe) verir
/// ve hiçbir şey oluşmaz (10.000.000,00 ve %100 kabul); aynı adla ikinci etkin kayıt harf farkı gözetmeden reddedilir (pasif olanın
/// adı serbest, ama onu etkinleştirmek reddedilir); pasif tarife/indirim plan formunda seçilemez; tarife tutarı değişince açılmış
/// planların tutarı değişmez.
/// </summary>
[Trait("Category", "Integration")]
public sealed class TariffAndDiscountTests
{
    private readonly TestDatabase database;

    public TariffAndDiscountTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Tariff_CreateUpdateDeactivateActivate_AndBounds()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);

        var (created, body, _) = await CreateTariffAsync(client, w.Founder, "  Yemek Ücreti  ", 10_000_000.00m);
        created.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("name").GetString().ShouldBe("Yemek Ücreti");
        Money(body, "amount").ShouldBe(10_000_000.00m);
        Guid id = body.GetProperty("id").GetGuid();

        var (updated, after, _) = await UpdateTariffAsync(client, w.Founder, id, "Yemek Ücreti 2027", 12_345.67m);
        updated.ShouldBe(HttpStatusCode.OK);
        Money(after, "amount").ShouldBe(12_345.67m);
        (await SetTariffActiveAsync(client, w.Founder, id, false)).Body.GetProperty("isActive").GetBoolean().ShouldBeFalse();
        (await SetTariffActiveAsync(client, w.Founder, id, true)).Body.GetProperty("isActive").GetBoolean().ShouldBeTrue();

        int before = (await FeeRowsAsync(factory, w.TenantId)).Count;
        foreach ((string? name, object? amount, string message) in new (string?, object?, string)[]
        {
            (string.Empty, 100m, "Ad zorunludur"),
            (new string('a', 101), 100m, "Ad en fazla 100 karakter olabilir"),
            ("Servis", 0m, "Tutar sıfırdan büyük olmalı"),
            ("Servis", -5m, "Tutar sıfırdan büyük olmalı"),
            ("Servis", 1.005m, "Tutar en fazla 2 ondalık olabilir"),
            ("Servis", 10_000_000.01m, "Tutar en fazla 10.000.000,00 TL olabilir"),
            ("Servis", "abc", "Tutar geçerli bir sayı olmalı"),
            ("Servis", null, "Tutar zorunludur"),
        })
        {
            var (status, problem, raw) = await CreateTariffAsync(client, w.Founder, name, amount);
            status.ShouldBe(HttpStatusCode.BadRequest, raw);
            Detail(problem).ShouldBe(message, raw);
        }

        (await UpdateTariffAsync(client, w.Founder, id, "Yemek Ücreti 2027", 0m)).Status.ShouldBe(HttpStatusCode.BadRequest);
        (await FeeRowsAsync(factory, w.TenantId)).Count.ShouldBe(before);
        Money((await TariffsAsync(client, w.Founder)).Body.EnumerateArray().Single(t => t.GetProperty("id").GetGuid() == id), "amount").ShouldBe(12_345.67m);
    }

    [Fact]
    public async Task Discount_CreateUpdateDeactivateActivate_AndBounds()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);

        var (created, body, _) = await CreateDiscountAsync(client, w.Founder, "Tam burs", 100);
        created.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("percent").GetInt32().ShouldBe(100);
        Guid id = body.GetProperty("id").GetGuid();
        (await UpdateDiscountAsync(client, w.Founder, id, "Yarım burs", 50)).Body.GetProperty("percent").GetInt32().ShouldBe(50);
        (await SetDiscountActiveAsync(client, w.Founder, id, false)).Body.GetProperty("isActive").GetBoolean().ShouldBeFalse();
        (await SetDiscountActiveAsync(client, w.Founder, id, true)).Body.GetProperty("isActive").GetBoolean().ShouldBeTrue();
        (await CreateDiscountAsync(client, w.Founder, "Personel", 1)).Status.ShouldBe(HttpStatusCode.OK);

        int before = (await DiscountRowsAsync(factory, w.TenantId)).Count;
        foreach ((string? name, object? percent, string message) in new (string?, object?, string)[]
        {
            ("  ", 10, "Ad zorunludur"),
            (new string('b', 101), 10, "Ad en fazla 100 karakter olabilir"),
            ("Erken kayıt", 0, "Yüzde 1 ile 100 arasında tam sayı olmalı"),
            ("Erken kayıt", 101, "Yüzde 1 ile 100 arasında tam sayı olmalı"),
            ("Erken kayıt", 12.5m, "Yüzde 1 ile 100 arasında tam sayı olmalı"),
            ("Erken kayıt", null, "Yüzde zorunludur"),
        })
        {
            var (status, problem, raw) = await CreateDiscountAsync(client, w.Founder, name, percent);
            status.ShouldBe(HttpStatusCode.BadRequest, raw);
            Detail(problem).ShouldBe(message, raw);
        }

        (await DiscountRowsAsync(factory, w.TenantId)).Count.ShouldBe(before);
    }

    [Fact]
    public async Task SameActiveName_IgnoringCase_Rejected_InactiveNameFree()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);

        var (dup, problem, _) = await CreateTariffAsync(client, w.Founder, "2026-2027 EĞİTİM ÜCRETİ", 5_000m);
        dup.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Bu adla etkin bir tarife zaten var.");
        Detail((await CreateDiscountAsync(client, w.Founder, "kardeş", 10)).Body).ShouldBe("Bu adla etkin bir indirim zaten var.");

        Guid servis = Id(await CreateTariffAsync(client, w.Founder, "Servis", 3_000m));
        (await UpdateTariffAsync(client, w.Founder, servis, "2026-2027 eğitim ücreti", 3_000m)).Status.ShouldBe(HttpStatusCode.BadRequest, "düzenleme de etkin adı alamaz");

        (await SetTariffActiveAsync(client, w.Founder, w.FeeId, false)).Status.ShouldBe(HttpStatusCode.OK);
        Guid again = Id(await CreateTariffAsync(client, w.Founder, "2026-2027 Eğitim Ücreti", 11_000m));
        var (reactivate, reProblem, _) = await SetTariffActiveAsync(client, w.Founder, w.FeeId, true);
        reactivate.ShouldBe(HttpStatusCode.BadRequest);
        Detail(reProblem).ShouldBe("Bu adla etkin bir tarife zaten var.");
        (await FeeRowsAsync(factory, w.TenantId)).Single(f => f.Id == w.FeeId).IsActive.ShouldBeFalse();
        again.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task InactiveTariffOrDiscount_NotSelectableInPlan_FeeChangeKeepsOpenedPlan()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        Guid plan = await PlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId, w.DiscountId));

        (await UpdateTariffAsync(client, w.Founder, w.FeeId, "2026-2027 Eğitim Ücreti", 20_000m)).Status.ShouldBe(HttpStatusCode.OK);
        (await UpdateDiscountAsync(client, w.Founder, w.DiscountId, "Kardeş", 50)).Status.ShouldBe(HttpStatusCode.OK);
        JsonElement opened = (await AccountAsync(client, w.Founder, w.Ali)).Body.GetProperty("plans").EnumerateArray().Single(p => p.GetProperty("id").GetGuid() == plan);
        Money(opened, "feeAmount").ShouldBe(10_000m);
        Money(opened, "discountAmount").ShouldBe(1_500m);
        opened.GetProperty("discountPercent").GetInt32().ShouldBe(15);
        Money(opened.GetProperty("totals"), "net").ShouldBe(8_500m);

        (await SetTariffActiveAsync(client, w.Founder, w.FeeId, false)).Status.ShouldBe(HttpStatusCode.OK);
        var (inactiveFee, feeProblem, _) = await CreatePlanAsync(client, w.Founder, PlanBody(w.School.Ayse, w.FeeId));
        inactiveFee.ShouldBe(HttpStatusCode.BadRequest);
        Detail(feeProblem).ShouldBe("Seçilen tarife pasif; etkin bir tarife seçin.");
        (await PreviewAsync(client, w.Founder, PlanBody(w.School.Ayse, w.FeeId))).Status.ShouldBe(HttpStatusCode.BadRequest);

        Guid other = Id(await CreateTariffAsync(client, w.Founder, "Servis", 4_000m));
        (await SetDiscountActiveAsync(client, w.Founder, w.DiscountId, false)).Status.ShouldBe(HttpStatusCode.OK);
        var (inactiveDiscount, discountProblem, _) = await CreatePlanAsync(client, w.Founder, PlanBody(w.School.Ayse, other, w.DiscountId));
        inactiveDiscount.ShouldBe(HttpStatusCode.BadRequest);
        Detail(discountProblem).ShouldBe("Seçilen indirim pasif; etkin bir indirim seçin.");
        (await PlanCountAsync(factory, w.School.Ayse)).ShouldBe(0);
    }
}
