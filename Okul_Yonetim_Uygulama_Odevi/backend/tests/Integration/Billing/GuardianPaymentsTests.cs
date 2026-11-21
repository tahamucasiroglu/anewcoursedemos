using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Billing.BillingTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Billing;

/// <summary>
/// Spec 0015 AC-11 (veli görünümü): veli seçili okuldaki bütün çocuklarının (farklı şubeler dahil) geçerli planlarını, taksit durumlarını,
/// tahsilatlarını (makbuz no, iptal durumu) ve toplam kalanı doğru görür; not, iptal gerekçesi ve kaydeden kişi veliye gitmez; planı
/// olmayan çocukta plan listesi boş (mobil "Henüz ödeme planı yok"); başka okulun çocuk kaydı gelmez; ödeme/düzenleme ucu yoktur.
/// </summary>
[Trait("Category", "Integration")]
public sealed class GuardianPaymentsTests
{
    private readonly TestDatabase database;

    public GuardianPaymentsTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Guardian_SeesAllChildrenAcrossBranches_TotalsAndPayments_NoStaffDetails()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        await LinkAsync(client, w.Founder, w.Cem, Guardian(w.School.AliMother.Email, relationship: "Mother"));
        Guid aliPlan = await PlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId, w.DiscountId));
        Guid cemFee = Id(await CreateTariffAsync(client, w.Founder, "Kurs", 1_000m));
        Guid cemPlan = await PlanAsync(client, w.Founder, PlanBody(w.Cem, cemFee, installmentCount: 2));
        await PaidAsync(client, w.Founder, aliPlan, 3_000m, method: "Cash", note: "Gizli not 123");
        (Guid wrong, _) = await PaidAsync(client, w.Founder, cemPlan, 250m, method: "BankTransfer");
        (await CancelPaymentAsync(client, w.Founder, wrong, "Gizli gerekçe 456")).Status.ShouldBe(HttpStatusCode.OK);
        Guid cancelledPlan = await PlanAsync(client, w.Founder, PlanBody(w.Ali, Id(await CreateTariffAsync(client, w.Founder, "Servis", 500m))));
        (await CancelPlanAsync(client, w.Founder, cancelledPlan)).Status.ShouldBe(HttpStatusCode.OK);

        string token = await AdministrationTokenAsync(client, w);
        var (status, view, raw) = await MyPaymentsAsync(client, token);
        status.ShouldBe(HttpStatusCode.OK);
        Money(view, "totalRemaining").ShouldBe(5_500m + 1_000m);
        JsonElement[] children = [.. view.GetProperty("children").EnumerateArray()];
        children.Select(c => c.GetProperty("fullName").GetString()).ShouldBe(["Ali Yılmaz", "Cem Arslan"]);
        children.Select(c => c.GetProperty("branchName").GetString()).ShouldBe(["Merkez", "Kadıköy"]);

        JsonElement ali = children[0];
        ali.GetProperty("plans").GetArrayLength().ShouldBe(1, "iptal plan veliye gösterilmez");
        JsonElement aliPlanView = ali.GetProperty("plans")[0];
        Money(aliPlanView.GetProperty("totals"), "net").ShouldBe(8_500m);
        Money(aliPlanView.GetProperty("totals"), "paid").ShouldBe(3_000m);
        States(aliPlanView).ShouldBe(["Paid", "PartiallyPaid", "Pending"]);
        JsonElement aliPayment = aliPlanView.GetProperty("payments")[0];
        aliPayment.GetProperty("receiptNo").GetInt64().ShouldBe(1);
        aliPayment.GetProperty("method").GetString().ShouldBe("Cash");
        aliPayment.GetProperty("cancelled").GetBoolean().ShouldBeFalse();

        JsonElement cemPayment = children[1].GetProperty("plans")[0].GetProperty("payments")[0];
        cemPayment.GetProperty("cancelled").GetBoolean().ShouldBeTrue();
        Money(children[1].GetProperty("totals"), "paid").ShouldBe(0m);

        raw.ShouldNotContain("Gizli not 123");
        raw.ShouldNotContain("Gizli gerekçe 456");
        raw.ShouldNotContain("recordedBy", Case.Insensitive);

        Money((await MyChildAsync(client, token, w.Cem)).Body.GetProperty("totals"), "remaining").ShouldBe(1_000m);
        using HttpResponseMessage noWrite = await client.SendAsync(Identity.IdentityTestData.Authorized(HttpMethod.Post, "/api/my/payments", token, new { }), TestContext.Current.CancellationToken);
        noWrite.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed, "veliye ödeme ucu yok");
    }

    [Fact]
    public async Task GuardianWithoutPlans_EmptyPlans_OtherSchoolChildNotIncluded()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        (SchoolSeed b, string bFounder) = await SchoolAsync(factory, client);
        Guid bChild = await CreateStudentAsync(client, bFounder, NewStudent(b.BranchId, "Deniz Başka"));
        await LinkAsync(client, bFounder, bChild, Guardian(w.School.AyseMother.Email));
        await PlanAsync(client, bFounder, PlanBody(bChild, Id(await CreateTariffAsync(client, bFounder, "B", 2_000m))));

        JsonElement view = (await MyPaymentsAsync(client, w.School.AyseMother.Token)).Body;
        Money(view, "totalRemaining").ShouldBe(0m);
        JsonElement child = view.GetProperty("children").EnumerateArray().Single();
        child.GetProperty("studentId").GetGuid().ShouldBe(w.School.Ayse);
        child.GetProperty("plans").GetArrayLength().ShouldBe(0);
        (await MyChildAsync(client, w.School.AyseMother.Token, bChild)).Status.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>Velinin yeni bağdan sonra yeniden girişi (yeni şubedeki veli rolü oturuma da girsin).</summary>
    private static Task<string> AdministrationTokenAsync(HttpClient client, BillingWorld w) =>
        Communication.CommunicationTestData.ReloginAsync(client, w.School.Class, w.School.AliMother);
}
