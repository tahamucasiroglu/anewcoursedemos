using System.Net;
using System.Text.Json;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Billing.BillingTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Billing;

/// <summary>
/// Spec 0015 AC-2 (izolasyon): A okulunun kurucusu ve muhasebesi B okulunun tarifesini, indirimini, öğrencisini, planını, tahsilatını,
/// makbuzunu ve carisini kimliğini bilerek bile göremez ve değiştiremez (404, listede yok); muhasebe kendi Muhasebe şubesi dışındaki
/// öğrenci için plan açamaz, tahsilat giremez, makbuz/cari açamaz (404) ve listesinde görmez — o şubede öğretmen olsa bile (oturumun
/// görünür şubesi yetmez); veli başka velinin çocuğunun ödeme ayrıntısına 404 alır.
/// </summary>
[Trait("Category", "Integration")]
public sealed class BillingIsolationTests
{
    private readonly TestDatabase database;

    public BillingIsolationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task OtherSchool_Everything404_AndNotListed()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        (SchoolSeed b, string bFounder) = await SchoolAsync(factory, client);
        Guid bStudent = await CreateStudentAsync(client, bFounder, NewStudent(b.BranchId, "Deniz Başka"));
        Guid bFee = Id(await CreateTariffAsync(client, bFounder, "B Ücreti", 5_000m));
        Guid bDiscount = Id(await CreateDiscountAsync(client, bFounder, "B İndirim", 10));
        Guid bPlan = await PlanAsync(client, bFounder, PlanBody(bStudent, bFee));
        (Guid bPayment, long bReceipt) = await PaidAsync(client, bFounder, bPlan, 1_000m);

        foreach (string token in new[] { w.Founder, w.Accountant })
        {
            Ids((await TariffsAsync(client, token)).Body).ShouldNotContain(bFee);
            Ids((await DiscountsAsync(client, token)).Body).ShouldNotContain(bDiscount);
            StudentIds((await StudentsAsync(client, token)).Body).ShouldNotContain(bStudent);
            (await AccountAsync(client, token, bStudent)).Status.ShouldBe(HttpStatusCode.NotFound);
            (await PreviewAsync(client, token, PlanBody(bStudent, bFee))).Status.ShouldBe(HttpStatusCode.NotFound);
            (await CreatePlanAsync(client, token, PlanBody(bStudent, w.FeeId))).Status.ShouldBe(HttpStatusCode.NotFound);
            (await CreatePlanAsync(client, token, PlanBody(w.School.Ayse, bFee))).Status.ShouldBe(HttpStatusCode.NotFound, "başka okulun tarifesi");
            (await CreatePlanAsync(client, token, PlanBody(w.School.Ayse, w.FeeId, bDiscount))).Status.ShouldBe(HttpStatusCode.NotFound, "başka okulun indirimi");
            (await CancelPlanAsync(client, token, bPlan)).Status.ShouldBe(HttpStatusCode.NotFound);
            (await PayAsync(client, token, PaymentBody(bPlan, 10m))).Status.ShouldBe(HttpStatusCode.NotFound);
            (await CancelPaymentAsync(client, token, bPayment)).Status.ShouldBe(HttpStatusCode.NotFound);
            (await ReceiptAsync(client, token, bPayment)).Status.ShouldBe(HttpStatusCode.NotFound);
        }

        (await UpdateTariffAsync(client, w.Founder, bFee, "Ele geçti", 1m)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await SetTariffActiveAsync(client, w.Founder, bFee, false)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await UpdateDiscountAsync(client, w.Founder, bDiscount, "Ele geçti", 99)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await SetDiscountActiveAsync(client, w.Founder, bDiscount, false)).Status.ShouldBe(HttpStatusCode.NotFound);

        JsonElement bAccount = (await AccountAsync(client, bFounder, bStudent)).Body;
        Money(bAccount.GetProperty("totals"), "paid").ShouldBe(1_000m);
        bAccount.GetProperty("payments")[0].GetProperty("cancelled").GetBoolean().ShouldBeFalse();
        bAccount.GetProperty("plans")[0].GetProperty("status").GetString().ShouldBe("Active");
        (await FeeRowsAsync(factory, b.TenantId)).ShouldHaveSingleItem().Name.ShouldBe("B Ücreti");
        (await DiscountRowsAsync(factory, b.TenantId)).ShouldHaveSingleItem().Percent.ShouldBe(10);
        (await PaymentCountAsync(factory, b.TenantId)).ShouldBe(1);
        bReceipt.ShouldBe(1);
    }

    [Fact]
    public async Task Accountant_OtherBranchStudent_404_EvenWhenTeacherThere_AndNotListed()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        Guid cemPlan = await PlanAsync(client, w.Founder, PlanBody(w.Cem, w.FeeId));
        (Guid cemPayment, _) = await PaidAsync(client, w.Founder, cemPlan, 500m);
        await PlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId));
        await AddMemberAsync(factory, w.AccountantEmail, w.TenantId, Role.Teacher, w.OtherBranchId);

        (await PreviewAsync(client, w.Accountant, PlanBody(w.Cem, w.FeeId))).Status.ShouldBe(HttpStatusCode.NotFound);
        (await CreatePlanAsync(client, w.Accountant, PlanBody(w.Cem, w.FeeId, w.DiscountId))).Status.ShouldBe(HttpStatusCode.NotFound);
        (await PayAsync(client, w.Accountant, PaymentBody(cemPlan, 100m))).Status.ShouldBe(HttpStatusCode.NotFound);
        (await CancelPlanAsync(client, w.Accountant, cemPlan)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await CancelPaymentAsync(client, w.Accountant, cemPayment)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await ReceiptAsync(client, w.Accountant, cemPayment)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await AccountAsync(client, w.Accountant, w.Cem)).Status.ShouldBe(HttpStatusCode.NotFound);

        JsonElement list = (await StudentsAsync(client, w.Accountant)).Body;
        StudentIds(list).ShouldBe([w.Ali]);
        list.GetProperty("branches").EnumerateArray().Select(b => b.GetProperty("id").GetGuid()).ShouldBe([w.BranchId]);
        StudentIds((await StudentsAsync(client, w.Accountant, $"?branchId={w.OtherBranchId}")).Body).ShouldBeEmpty();
        StudentIds((await StudentsAsync(client, w.Founder)).Body).ShouldBe([w.Ali, w.Cem], ignoreOrder: true);

        JsonElement cem = (await AccountAsync(client, w.Founder, w.Cem)).Body;
        Money(cem.GetProperty("totals"), "paid").ShouldBe(500m);
        cem.GetProperty("payments")[0].GetProperty("cancelled").GetBoolean().ShouldBeFalse();
        (await PlanCountAsync(factory, w.Cem)).ShouldBe(1);
    }

    [Fact]
    public async Task Guardian_OtherGuardiansChild_404_OnlyOwnChildrenListed()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        await PlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId));
        await PlanAsync(client, w.Founder, PlanBody(w.School.Ayse, w.FeeId));

        (await MyChildAsync(client, w.School.AliMother.Token, w.School.Ayse)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await MyChildAsync(client, w.School.AliMother.Token, w.Cem)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await MyChildAsync(client, w.School.AliMother.Token, w.Ali)).Status.ShouldBe(HttpStatusCode.OK);
        (await MyPaymentsAsync(client, w.School.AliMother.Token)).Body.GetProperty("children").EnumerateArray()
            .Select(c => c.GetProperty("studentId").GetGuid()).ShouldBe([w.Ali]);
    }

    private static Guid[] Ids(JsonElement items) => [.. items.EnumerateArray().Select(i => i.GetProperty("id").GetGuid())];

    private static Guid[] StudentIds(JsonElement page) => [.. page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("studentId").GetGuid())];
}
