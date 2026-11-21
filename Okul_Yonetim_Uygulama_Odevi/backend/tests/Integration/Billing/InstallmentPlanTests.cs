using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Billing.BillingTestData;

namespace OkulYonetim.Tests.Integration.Billing;

/// <summary>
/// Spec 0015 AC-4 (plan hesabı, uçtan uca): 10.000,00 TL + %15 + 3 taksit → indirim 1.500,00, net 8.500,00, taksitler 2.833,33 / 2.833,33 /
/// 2.833,34 (önizleme ve kayıt aynı; önizleme hiçbir şey yazmaz); 1.000,00 / 7 taksit kuruş farkı son taksitte, toplam = net; %100 indirim
/// "Net tutar 0 TL; tam burslu öğrenci için plan açılmaz." ile reddedilir; 31 Ocak ilk vadede Şubat vadesi ayın son günü; taksit sayısı
/// 0/13, dünkü ya da 366 gün sonraki ilk vade (365 kabul), ayrılmış öğrenci ve aynı tarifeden ikinci geçerli plan reddedilir — hiçbir
/// şey kaydedilmez; farklı tarifeden ikinci plan açılır; muhasebe kendi şubesinde plan açar. Taksiti 0,01 TL'nin altına düşüren plan
/// (0,10 TL / 12 taksit) önizlemede de açmada da Türkçe iş kuralıyla 400 alır, hiçbir şey kaydedilmez; 0,10 TL / 10 taksit sınırda açılır
/// (0015 review R1-1).
/// </summary>
[Trait("Category", "Integration")]
public sealed class InstallmentPlanTests
{
    private readonly TestDatabase database;

    public InstallmentPlanTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task TenThousandFifteenPercentThree_PreviewEqualsSavedPlan()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);

        var (previewStatus, preview, _) = await PreviewAsync(client, w.Accountant, PlanBody(w.Ali, w.FeeId, w.DiscountId));
        previewStatus.ShouldBe(HttpStatusCode.OK);
        Money(preview, "discountAmount").ShouldBe(1_500.00m);
        Money(preview, "net").ShouldBe(8_500.00m);
        Amounts(preview.GetProperty("installments"), "amount").ShouldBe([2_833.33m, 2_833.33m, 2_833.34m]);
        (await PlanCountAsync(factory, w.Ali)).ShouldBe(0, "önizleme kaydetmez");

        Guid planId = await PlanAsync(client, w.Accountant, PlanBody(w.Ali, w.FeeId, w.DiscountId));
        JsonElement plan = (await AccountAsync(client, w.Accountant, w.Ali)).Body.GetProperty("plans").EnumerateArray().Single();
        plan.GetProperty("id").GetGuid().ShouldBe(planId);
        plan.GetProperty("feeName").GetString().ShouldBe("2026-2027 Eğitim Ücreti");
        plan.GetProperty("discountName").GetString().ShouldBe("Kardeş");
        Money(plan.GetProperty("totals"), "net").ShouldBe(8_500.00m);
        Amounts(plan.GetProperty("installments"), "amount").ShouldBe([2_833.33m, 2_833.33m, 2_833.34m]);
        plan.GetProperty("installments").EnumerateArray().Select(i => i.GetProperty("dueDate").GetString()).ShouldBe(["2026-12-01", "2027-01-01", "2027-02-01"]);
        States(plan).ShouldBe(["Pending", "Pending", "Pending"]);
    }

    [Fact]
    public async Task ThousandSeven_LastHoldsPenny_JanuaryThirtyFirst_FebruaryLastDay_HundredPercentRejected()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        Guid thousand = Id(await CreateTariffAsync(client, w.Founder, "Kitap", 1_000.00m));

        JsonElement seven = (await PreviewAsync(client, w.Founder, PlanBody(w.Ali, thousand, installmentCount: 7))).Body;
        decimal[] amounts = Amounts(seven.GetProperty("installments"), "amount");
        amounts.Take(6).ShouldAllBe(a => a == 142.85m);
        amounts[6].ShouldBe(142.90m);
        amounts.Sum().ShouldBe(1_000.00m);

        JsonElement january = (await PreviewAsync(client, w.Founder, PlanBody(w.Ali, thousand, installmentCount: 3, firstDueDate: "2027-01-31"))).Body;
        january.GetProperty("installments").EnumerateArray().Select(i => i.GetProperty("dueDate").GetString()).ShouldBe(["2027-01-31", "2027-02-28", "2027-03-31"]);

        Guid full = Id(await CreateDiscountAsync(client, w.Founder, "Tam burs", 100));
        var (status, problem, _) = await CreatePlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId, full));
        status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Net tutar 0 TL; tam burslu öğrenci için plan açılmaz.");
        Detail((await PreviewAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId, full))).Body).ShouldBe("Net tutar 0 TL; tam burslu öğrenci için plan açılmaz.");
        (await PlanCountAsync(factory, w.Ali)).ShouldBe(0);
    }

    [Fact]
    public async Task InvalidCountDueLeftStudentAndDuplicate_Rejected_NothingSaved()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);

        foreach ((object body, string message) in new (object, string)[]
        {
            (PlanBody(w.Ali, w.FeeId, installmentCount: 0), "Taksit sayısı 1 ile 12 arasında olmalı"),
            (PlanBody(w.Ali, w.FeeId, installmentCount: 13), "Taksit sayısı 1 ile 12 arasında olmalı"),
            (PlanBody(w.Ali, w.FeeId, installmentCount: null), "Taksit sayısı 1 ile 12 arasında olmalı"),
            (PlanBody(w.Ali, w.FeeId, firstDueDate: "2026-11-19"), "İlk vade bugün ile 365 gün sonrası arasında olmalı"),
            (PlanBody(w.Ali, w.FeeId, firstDueDate: "2027-11-21"), "İlk vade bugün ile 365 gün sonrası arasında olmalı"),
            (PlanBody(w.Ali, w.FeeId, firstDueDate: null), "İlk vade zorunludur"),
            (PlanBody(w.Ali, Guid.Empty), "Tarife zorunludur"),
        })
        {
            var (status, problem, raw) = await CreatePlanAsync(client, w.Founder, body);
            status.ShouldBe(HttpStatusCode.BadRequest, raw);
            Detail(problem).ShouldBe(message, raw);
        }

        (await PlanCountAsync(factory, w.Ali)).ShouldBe(0);
        (await CreatePlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId, installmentCount: 12, firstDueDate: "2027-11-20"))).Status.ShouldBe(HttpStatusCode.OK, "bugün + 365 ve 12 taksit sınırda kabul");

        var (dup, dupProblem, _) = await CreatePlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId, w.DiscountId));
        dup.ShouldBe(HttpStatusCode.BadRequest);
        Detail(dupProblem).ShouldBe("Bu öğrencinin bu tarifeden geçerli bir planı var.");
        Guid lunch = Id(await CreateTariffAsync(client, w.Founder, "Yemek", 2_400m));
        (await CreatePlanAsync(client, w.Founder, PlanBody(w.Ali, lunch, firstDueDate: Today))).Status.ShouldBe(HttpStatusCode.OK, "farklı tarifeden ikinci plan; ilk vade bugün");
        (await PlanCountAsync(factory, w.Ali)).ShouldBe(2);

        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/students/{w.School.Berk}/deactivate", w.Founder)).ShouldBe(HttpStatusCode.OK);
        var (left, leftProblem, _) = await CreatePlanAsync(client, w.Founder, PlanBody(w.School.Berk, w.FeeId));
        left.ShouldBe(HttpStatusCode.BadRequest);
        Detail(leftProblem).ShouldBe("Ayrılmış öğrenci için plan açılamaz.");
        (await PlanCountAsync(factory, w.School.Berk)).ShouldBe(0);
    }

    [Fact]
    public async Task NetBelowOneKurusPerInstallment_PreviewAndCreateRejectedInTurkish_NothingSaved()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        Guid tiny = Id(await CreateTariffAsync(client, w.Founder, "Kırtasiye", 0.10m));
        const string message = "Taksit tutarı 0,01 TL'den az olamaz; taksit sayısını azaltın.";

        var (previewStatus, previewProblem, previewRaw) = await PreviewAsync(client, w.Founder, PlanBody(w.Ali, tiny, installmentCount: 12));
        previewStatus.ShouldBe(HttpStatusCode.BadRequest, previewRaw);
        Detail(previewProblem).ShouldBe(message, previewRaw);

        var (status, problem, raw) = await CreatePlanAsync(client, w.Accountant, PlanBody(w.Ali, tiny, installmentCount: 11));
        status.ShouldBe(HttpStatusCode.BadRequest, raw);
        Detail(problem).ShouldBe(message, raw);
        (await PlanCountAsync(factory, w.Ali)).ShouldBe(0);

        JsonElement ten = (await PreviewAsync(client, w.Founder, PlanBody(w.Ali, tiny, installmentCount: 10))).Body;
        Amounts(ten.GetProperty("installments"), "amount").ShouldAllBe(a => a == 0.01m);
        (await CreatePlanAsync(client, w.Founder, PlanBody(w.Ali, tiny, installmentCount: 10))).Status.ShouldBe(HttpStatusCode.OK, "0,10 TL / 10 taksit sınırda kabul");
        (await PlanCountAsync(factory, w.Ali)).ShouldBe(1);
    }
}
