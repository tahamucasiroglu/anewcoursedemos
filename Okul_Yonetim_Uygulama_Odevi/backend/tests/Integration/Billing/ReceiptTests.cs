using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Billing.BillingTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Billing;

/// <summary>
/// Spec 0015 AC-8 (makbuz): okulun ilk tahsilatı 1 numarayı alır, sonrakiler boşluksuz artar, iptal edilenin numarası yeniden verilmez,
/// iki okulun numaraları birbirinden bağımsızdır; makbuz okul ve şube adı, makbuz no, tarih, öğrenci ad-soyad, plan adı, tutar, yöntem,
/// dağıldığı taksitler ve kaydeden kişinin adını taşır; TC kimlik no, IBAN ve sağlık bilgisi taşımaz ("Bu belge fatura yerine geçmez."
/// ve yazdırma görünümü web testinde).
/// </summary>
[Trait("Category", "Integration")]
public sealed class ReceiptTests
{
    private readonly TestDatabase database;

    public ReceiptTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Numbers_StartAtOne_NoGap_CancelledNotReused_IndependentPerSchool()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        Guid ali = await PlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId));
        Guid ayse = await PlanAsync(client, w.Accountant, PlanBody(w.School.Ayse, w.FeeId));

        (await PaidAsync(client, w.Founder, ali, 100m)).ReceiptNo.ShouldBe(1);
        (Guid second, long two) = await PaidAsync(client, w.Accountant, ayse, 100m);
        two.ShouldBe(2);
        (await CancelPaymentAsync(client, w.Founder, second)).Status.ShouldBe(HttpStatusCode.OK);
        (await PaidAsync(client, w.Founder, ali, 100m)).ReceiptNo.ShouldBe(3, "iptal edilen 2 yeniden verilmez");

        (SchoolSeed b, string bFounder) = await SchoolAsync(factory, client);
        Guid bStudent = await CreateStudentAsync(client, bFounder, NewStudent(b.BranchId, "Deniz Başka"));
        Guid bPlan = await PlanAsync(client, bFounder, PlanBody(bStudent, Id(await CreateTariffAsync(client, bFounder, "B", 1_000m))));
        (await PaidAsync(client, bFounder, bPlan, 10m)).ReceiptNo.ShouldBe(1, "okul başına ayrı sıra");
        (await PaidAsync(client, w.Founder, ayse, 100m)).ReceiptNo.ShouldBe(4);
    }

    [Fact]
    public async Task Receipt_CarriesRequiredFields_NoNationalIdIbanOrHealth()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        Guid plan = await PlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId, w.DiscountId));
        (Guid payment, _) = await PaidAsync(client, w.Accountant, plan, 3_000m, method: "CardPos", note: "POS fişi 77");

        var (status, slip, raw) = await ReceiptAsync(client, w.Accountant, payment);
        status.ShouldBe(HttpStatusCode.OK);
        slip.GetProperty("schoolName").GetString().ShouldBe(w.School.Class.School.SchoolName);
        slip.GetProperty("branchName").GetString().ShouldBe("Merkez");
        slip.GetProperty("receiptNo").GetInt64().ShouldBe(1);
        slip.GetProperty("date").GetString().ShouldBe(Today);
        slip.GetProperty("studentName").GetString().ShouldBe("Ali Yılmaz");
        slip.GetProperty("planName").GetString().ShouldBe("2026-2027 Eğitim Ücreti");
        Money(slip, "amount").ShouldBe(3_000m);
        slip.GetProperty("method").GetString().ShouldBe("CardPos");
        slip.GetProperty("recordedByName").GetString().ShouldBe("Test Kişi");
        slip.GetProperty("cancelled").GetBoolean().ShouldBeFalse();
        JsonElement[] allocations = [.. slip.GetProperty("allocations").EnumerateArray()];
        allocations.Select(a => a.GetProperty("sequence").GetInt32()).ShouldBe([1, 2]);
        allocations.Select(a => a.GetProperty("amount").GetDecimal()).ShouldBe([2_833.33m, 166.67m]);
        allocations[0].GetProperty("dueDate").GetString().ShouldBe("2026-12-01");

        string lower = raw.ToLowerInvariant();
        foreach (string forbidden in new[] { "iban", "nationalid", "tckn", "health", "allergy", "pos fişi" })
        {
            lower.ShouldNotContain(forbidden);
        }
    }
}
