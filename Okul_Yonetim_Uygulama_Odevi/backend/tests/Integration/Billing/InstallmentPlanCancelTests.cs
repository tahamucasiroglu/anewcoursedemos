using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Billing.BillingTestData;

namespace OkulYonetim.Tests.Integration.Billing;

/// <summary>
/// Spec 0015 AC-5 (plan iptali): tahsilatı olmayan plan iptal edilir, listede "İptal" (Cancelled) görünür ve cariye, gecikmeye ve
/// hatırlatmaya girmez (vadesi geçse de); geçerli tahsilatı olan plan "Bu planda tahsilat var; önce tahsilatları iptal edin." ile
/// reddedilir; tahsilatları iptal edilmiş plan iptal edilebilir; iptal edilmiş plan yeniden iptal edilemez ve aynı tarifeden yeni plan açılabilir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class InstallmentPlanCancelTests
{
    private readonly TestDatabase database;

    public InstallmentPlanCancelTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task PlanWithoutPayments_Cancelled_OutOfLedgerOverdueAndReminders()
    {
        FakeTimeProvider time = new(Attendance.AttendanceTestData.Morning);
        CapturingPushNotifier pushes = new();
        using OkulApiFactory factory = BillingFactory(this.database, pushes, time);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        Guid plan = await PlanAsync(client, w.Founder, PlanBody(w.Ali, w.FeeId, firstDueDate: Today));

        (await CancelPlanAsync(client, w.Founder, plan)).Status.ShouldBe(HttpStatusCode.OK);
        var (again, problem, _) = await CancelPlanAsync(client, w.Founder, plan);
        again.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Plan zaten iptal edilmiş.");

        time.Advance(TimeSpan.FromDays(2));
        string founder = await FounderTokenAsync(client, w);
        JsonElement account = (await AccountAsync(client, founder, w.Ali)).Body;
        account.GetProperty("plans")[0].GetProperty("status").GetString().ShouldBe("Cancelled");
        Money(account.GetProperty("totals"), "net").ShouldBe(0m);
        Money(account.GetProperty("totals"), "overdue").ShouldBe(0m);
        account.GetProperty("movements").GetArrayLength().ShouldBe(0);
        Money((await StudentsAsync(client, founder, "?overdueOnly=true")).Body.GetProperty("totals"), "overdue").ShouldBe(0m);
        (await RunRemindersAsync(factory, w.TenantId)).ShouldBe(0);
        pushes.Sent.ShouldBeEmpty();
        (await ReminderNoticeCountAsync(factory, w.School.AliMother.UserId)).ShouldBe(0);

        (await CreatePlanAsync(client, founder, PlanBody(w.Ali, w.FeeId, firstDueDate: "2026-12-25"))).Status.ShouldBe(HttpStatusCode.OK, "iptal plan aynı tarifeyi tutmaz");
    }

    [Fact]
    public async Task PlanWithValidPayment_Rejected_AfterPaymentCancelled_Allowed()
    {
        using OkulApiFactory factory = BillingFactory(this.database);
        using HttpClient client = factory.CreateClient();
        BillingWorld w = await BillingWorldAsync(factory, client);
        Guid plan = await PlanAsync(client, w.Accountant, PlanBody(w.Ali, w.FeeId));
        (Guid payment, _) = await PaidAsync(client, w.Accountant, plan, 1_000m);

        var (rejected, problem, _) = await CancelPlanAsync(client, w.Accountant, plan);
        rejected.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Bu planda tahsilat var; önce tahsilatları iptal edin.");
        (await AccountAsync(client, w.Accountant, w.Ali)).Body.GetProperty("plans")[0].GetProperty("status").GetString().ShouldBe("Active");

        (await CancelPaymentAsync(client, w.Accountant, payment)).Status.ShouldBe(HttpStatusCode.OK);
        (await CancelPlanAsync(client, w.Accountant, plan)).Status.ShouldBe(HttpStatusCode.OK);
        (await AccountAsync(client, w.Accountant, w.Ali)).Body.GetProperty("plans")[0].GetProperty("status").GetString().ShouldBe("Cancelled");
        var (pay, payProblem, _) = await PayAsync(client, w.Accountant, PaymentBody(plan, 10m));
        pay.ShouldBe(HttpStatusCode.BadRequest);
        Detail(payProblem).ShouldBe("İptal edilmiş plana tahsilat girilemez.");
    }
}
