using System.Net;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Billing.BillingTestData;

namespace OkulYonetim.Tests.Integration.Billing;

/// <summary>
/// Spec 0015 AC-12 (audit): tarife ve indirim ekleme/düzenleme/pasifleştirme/etkinleştirme, plan açma/iptal, tahsilat kaydı/iptali audit'e
/// yazılır — kim (kişi), ne (olay), hangi kayıt (tür + kimlik), ne zaman, IP; ayrıntıda yalnızca kimlik ve makbuz no (tutar, not,
/// gerekçe, ad yok). Reddedilen işlem (doğrulama, iş kuralı, yetki) audit'e başarı olarak yazılmaz.
/// </summary>
[Trait("Category", "Integration")]
public sealed class BillingAuditTests
{
    private const string Ip = "203.0.113.15";
    private readonly TestDatabase database;

    public BillingAuditTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task EveryBillingWrite_Audited_WithActorTargetTimeAndIp_RejectionsNotAudited()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        client.DefaultRequestHeaders.Add("X-Forwarded-For", Ip);

        Guid fee = Id(await CreateTariffAsync(client, w.Founder, "Kurs", 2_000m));
        (await UpdateTariffAsync(client, w.Founder, fee, "Kurs 2", 2_100m)).Status.ShouldBe(HttpStatusCode.OK);
        (await SetTariffActiveAsync(client, w.Founder, fee, false)).Status.ShouldBe(HttpStatusCode.OK);
        (await SetTariffActiveAsync(client, w.Founder, fee, true)).Status.ShouldBe(HttpStatusCode.OK);
        Guid discount = Id(await CreateDiscountAsync(client, w.Founder, "Personel", 20));
        (await UpdateDiscountAsync(client, w.Founder, discount, "Personel indirimi", 25)).Status.ShouldBe(HttpStatusCode.OK);
        (await SetDiscountActiveAsync(client, w.Founder, discount, false)).Status.ShouldBe(HttpStatusCode.OK);
        (await SetDiscountActiveAsync(client, w.Founder, discount, true)).Status.ShouldBe(HttpStatusCode.OK);
        Guid plan = await PlanAsync(client, w.Accountant, PlanBody(w.Ali, fee, discount));
        (Guid payment, long receipt) = await PaidAsync(client, w.Accountant, plan, 300m, note: "Gizli not");
        (await CancelPaymentAsync(client, w.Accountant, payment, "Gizli gerekçe")).Status.ShouldBe(HttpStatusCode.OK);
        (await CancelPlanAsync(client, w.Accountant, plan)).Status.ShouldBe(HttpStatusCode.OK);

        int before = (await BillingAuditAsync(factory, w.TenantId)).Count;
        (await CreateTariffAsync(client, w.Founder, string.Empty, 1m)).Status.ShouldBe(HttpStatusCode.BadRequest);
        (await CreateTariffAsync(client, w.Founder, "Kurs 2", 5m)).Status.ShouldBe(HttpStatusCode.BadRequest);
        (await CreateTariffAsync(client, w.Accountant, "Muhasebe tarifesi", 5m)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await CreatePlanAsync(client, w.School.Class.Teacher, PlanBody(w.Ali, fee))).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await CancelPlanAsync(client, w.Accountant, plan)).Status.ShouldBe(HttpStatusCode.BadRequest);
        (await PayAsync(client, w.Accountant, PaymentBody(plan, 1m))).Status.ShouldBe(HttpStatusCode.BadRequest);
        (await CancelPaymentAsync(client, w.Accountant, payment, "tekrar")).Status.ShouldBe(HttpStatusCode.BadRequest);

        List<AuditEntry> audit = [.. (await BillingAuditAsync(factory, w.TenantId)).Where(a => a.IpAddress == Ip)];
        (await BillingAuditAsync(factory, w.TenantId)).Count.ShouldBe(before, "reddedilenler audit'e yazılmaz");
        audit.Select(a => a.Action).ShouldBe(
        [
            AuditAction.FeeScheduleCreated, AuditAction.FeeScheduleUpdated, AuditAction.FeeScheduleDeactivated, AuditAction.FeeScheduleActivated,
            AuditAction.DiscountCreated, AuditAction.DiscountUpdated, AuditAction.DiscountDeactivated, AuditAction.DiscountActivated,
            AuditAction.InstallmentPlanCreated, AuditAction.PaymentRecorded, AuditAction.PaymentCancelled, AuditAction.InstallmentPlanCancelled,
        ], ignoreOrder: true);
        audit.Where(a => a.TargetType is "FeeSchedule" or "DiscountDefinition").Count().ShouldBe(8);
        audit.Where(a => a.TargetType is "FeeSchedule" or "DiscountDefinition").ShouldAllBe(a => a.ActorUserId == w.School.Class.School.FounderId);
        audit.Where(a => a.TargetType is "InstallmentPlan" or "Payment").ShouldAllBe(a => a.ActorUserId == w.AccountantId);
        audit.ShouldAllBe(a => a.OccurredAt == Attendance.AttendanceTestData.Morning && a.TenantId == w.TenantId);
        audit.Where(a => a.TargetType == "FeeSchedule").ShouldAllBe(a => a.TargetId == fee);
        audit.Where(a => a.TargetType == "DiscountDefinition").ShouldAllBe(a => a.TargetId == discount);
        audit.Where(a => a.Action is AuditAction.InstallmentPlanCreated or AuditAction.InstallmentPlanCancelled).ShouldAllBe(a => a.TargetType == "InstallmentPlan" && a.TargetId == plan);
        audit.Where(a => a.Action is AuditAction.PaymentRecorded or AuditAction.PaymentCancelled).ShouldAllBe(a => a.TargetType == "Payment" && a.TargetId == payment
            && a.Detail == $"plan={plan:D};student={w.Ali:D};receipt={receipt}");
        audit.ShouldAllBe(a => a.Detail == null || (!a.Detail.Contains("300.00") && !a.Detail.Contains("2000.00") && !a.Detail.Contains("Gizli") && !a.Detail.Contains("Kurs")));
    }
}
