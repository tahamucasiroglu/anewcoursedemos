using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OkulYonetim.Application.Features.Billing.Reminders;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Billing;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Billing;

/// <summary>
/// Spec 0015 test yardımcıları: API fabrikası 0011'in <c>CommunicationTestData.Factory</c>'si (sabit saat 2026-11-20 09:00 UTC = Türkiye
/// 12:00; istenirse bildirimi yakalayan gönderici ve ileri alınabilen sahte saat), hazır okul (<see cref="BillingWorld"/>), tarife,
/// indirim, plan, tahsilat, makbuz, cari ve veli uçları, hatırlatma çalışması (işçi testlerde kapalı — okul çalışması doğrudan) ve
/// veritabanı sayaçları. Tutarlar JSON'a <c>decimal</c> olarak (ham metniyle) gider.
/// </summary>
public static class BillingTestData
{
    public const string Today = "2026-11-20";

    /// <summary>Spec 0015 audit olayları (69–80). Eylem kolonu metin olduğu için aralık değil liste ile süzülür.</summary>
    public static readonly AuditAction[] BillingActions = [.. Enum.GetValues<AuditAction>().Where(a => a >= AuditAction.FeeScheduleCreated && a <= AuditAction.PaymentCancelled)];

    public static OkulApiFactory BillingFactory(TestDatabase database, IPushNotifier? notifier = null, TimeProvider? time = null) =>
        CommunicationTestData.Factory(database, notifier, time);

    public static async Task<BillingWorld> BillingWorldAsync(OkulApiFactory factory, HttpClient client)
    {
        CommunicationWorld w = await CommunicationTestData.WorldAsync(factory, client);
        Guid tenantId = w.Class.School.TenantId;
        string accountantEmail = UniqueEmail("muhasebe");
        Guid accountantId = await AddMemberAsync(factory, accountantEmail, tenantId, Role.Accounting, w.Class.School.BranchId);
        string accountant = await TokenForSchoolAsync(client, accountantEmail, tenantId);
        Guid otherBranch = await AddBranchAsync(factory, tenantId, "Kadıköy");
        Guid cem = await CreateStudentAsync(client, w.Class.Founder, NewStudent(otherBranch, "Cem Arslan"));
        ParentAccount cemMother = await CommunicationTestData.ParentAsync(client, w.Class, cem, "Mother");
        Guid fee = Id(await CreateTariffAsync(client, w.Class.Founder, "2026-2027 Eğitim Ücreti", 10_000.00m));
        Guid discount = Id(await CreateDiscountAsync(client, w.Class.Founder, "Kardeş", 15));
        return new BillingWorld(w, accountantId, accountantEmail, accountant, otherBranch, cem, cemMother, fee, discount);
    }

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> TariffsAsync(HttpClient client, string token) =>
        CallAsync(client, HttpMethod.Get, "/api/billing/tariffs", token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> CreateTariffAsync(HttpClient client, string token, string? name, object? amount) =>
        CallAsync(client, HttpMethod.Post, "/api/billing/tariffs", token, new { name, amount });

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> UpdateTariffAsync(HttpClient client, string token, Guid id, string? name, object? amount) =>
        CallAsync(client, HttpMethod.Put, $"/api/billing/tariffs/{id}", token, new { name, amount });

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> SetTariffActiveAsync(HttpClient client, string token, Guid id, bool active) =>
        CallAsync(client, HttpMethod.Post, $"/api/billing/tariffs/{id}/{(active ? "activate" : "deactivate")}", token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> DiscountsAsync(HttpClient client, string token) =>
        CallAsync(client, HttpMethod.Get, "/api/billing/discounts", token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> CreateDiscountAsync(HttpClient client, string token, string? name, object? percent) =>
        CallAsync(client, HttpMethod.Post, "/api/billing/discounts", token, new { name, percent });

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> UpdateDiscountAsync(HttpClient client, string token, Guid id, string? name, object? percent) =>
        CallAsync(client, HttpMethod.Put, $"/api/billing/discounts/{id}", token, new { name, percent });

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> SetDiscountActiveAsync(HttpClient client, string token, Guid id, bool active) =>
        CallAsync(client, HttpMethod.Post, $"/api/billing/discounts/{id}/{(active ? "activate" : "deactivate")}", token);

    public static object PlanBody(Guid studentId, Guid feeId, Guid? discountId = null, int? installmentCount = 3, string? firstDueDate = "2026-12-01") =>
        new { studentId, feeScheduleId = feeId, discountId, installmentCount, firstDueDate };

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> PreviewAsync(HttpClient client, string token, object body) =>
        CallAsync(client, HttpMethod.Post, "/api/billing/plans/preview", token, body);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> CreatePlanAsync(HttpClient client, string token, object body) =>
        CallAsync(client, HttpMethod.Post, "/api/billing/plans", token, body);

    public static async Task<Guid> PlanAsync(HttpClient client, string token, object body) => Id(await CreatePlanAsync(client, token, body));

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> CancelPlanAsync(HttpClient client, string token, Guid planId) =>
        CallAsync(client, HttpMethod.Post, $"/api/billing/plans/{planId}/cancel", token);

    public static object PaymentBody(Guid planId, object? amount, string? date = Today, string? method = "Cash", string? note = null) =>
        new { planId, amount, date, method, note };

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> PayAsync(HttpClient client, string token, object body) =>
        CallAsync(client, HttpMethod.Post, "/api/billing/payments", token, body);

    /// <summary>Tahsilat kaydı; kimlik ve makbuz numarası.</summary>
    public static async Task<(Guid Id, long ReceiptNo)> PaidAsync(HttpClient client, string token, Guid planId, decimal amount, string date = Today, string method = "Cash", string? note = null)
    {
        var (status, json, raw) = await PayAsync(client, token, PaymentBody(planId, amount, date, method, note));
        if (status != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"tahsilat kaydedilemedi: {status} {raw}");
        }

        return (json.GetProperty("id").GetGuid(), json.GetProperty("receiptNo").GetInt64());
    }

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> CancelPaymentAsync(HttpClient client, string token, Guid paymentId, string? reason = "Yanlış öğrenciye girildi") =>
        CallAsync(client, HttpMethod.Post, $"/api/billing/payments/{paymentId}/cancel", token, new { reason });

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> ReceiptAsync(HttpClient client, string token, Guid paymentId) =>
        CallAsync(client, HttpMethod.Get, $"/api/billing/payments/{paymentId}/receipt", token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> StudentsAsync(HttpClient client, string token, string query = "") =>
        CallAsync(client, HttpMethod.Get, "/api/billing/students" + query, token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> AccountAsync(HttpClient client, string token, Guid studentId) =>
        CallAsync(client, HttpMethod.Get, $"/api/billing/students/{studentId}", token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> MyPaymentsAsync(HttpClient client, string token) =>
        CallAsync(client, HttpMethod.Get, "/api/my/payments", token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> MyChildAsync(HttpClient client, string token, Guid studentId) =>
        CallAsync(client, HttpMethod.Get, $"/api/my/payments/{studentId}", token);

    /// <summary>Sahte saat ileri alınınca (erişim anahtarı süresi dolar) kurucunun yeniden girişi.</summary>
    public static Task<string> FounderTokenAsync(HttpClient client, BillingWorld w) =>
        TokenForSchoolAsync(client, w.School.Class.School.FounderEmail, w.TenantId);

    /// <summary>Sahte saat ileri alınınca muhasebenin yeniden girişi.</summary>
    public static Task<string> AccountantTokenAsync(HttpClient client, BillingWorld w) =>
        TokenForSchoolAsync(client, w.AccountantEmail, w.TenantId);

    /// <summary>Bir okulun hatırlatma çalışması (arka plan işçisinin okul başına yaptığı): yeni DI kapsamı + sistem oturumu.</summary>
    public static async Task<int> RunRemindersAsync(OkulApiFactory factory, Guid tenantId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<OverdueReminderRunner>().RunAsync(tenantId, TestContext.Current.CancellationToken);
    }

    public static Guid Id((HttpStatusCode Status, JsonElement Body, string Raw) response) =>
        response.Status == HttpStatusCode.OK
            ? response.Body.GetProperty("id").GetGuid()
            : throw new InvalidOperationException($"istek başarısız: {response.Status} {response.Raw}");

    public static decimal Money(JsonElement element, string property) => element.GetProperty(property).GetDecimal();

    public static decimal[] Amounts(JsonElement items, string property) => [.. items.EnumerateArray().Select(i => i.GetProperty(property).GetDecimal())];

    public static string[] States(JsonElement plan) => [.. plan.GetProperty("installments").EnumerateArray().Select(i => i.GetProperty("state").GetString()!)];

    public static Task<int> PlanCountAsync(OkulApiFactory factory, Guid studentId) =>
        QueryAsync(factory, c => c.InstallmentPlans.IgnoreQueryFilters().CountAsync(p => p.StudentId == studentId));

    public static Task<int> PaymentCountAsync(OkulApiFactory factory, Guid tenantId) =>
        QueryAsync(factory, c => c.Payments.IgnoreQueryFilters().CountAsync(p => p.TenantId == tenantId));

    public static Task<List<FeeSchedule>> FeeRowsAsync(OkulApiFactory factory, Guid tenantId) =>
        QueryAsync(factory, c => c.FeeSchedules.IgnoreQueryFilters().Where(f => f.TenantId == tenantId).ToListAsync());

    public static Task<List<DiscountDefinition>> DiscountRowsAsync(OkulApiFactory factory, Guid tenantId) =>
        QueryAsync(factory, c => c.DiscountDefinitions.IgnoreQueryFilters().Where(d => d.TenantId == tenantId).ToListAsync());

    public static Task<List<AuditEntry>> BillingAuditAsync(OkulApiFactory factory, Guid tenantId) =>
        QueryAsync(factory, c => c.AuditEntries.Where(a => a.TenantId == tenantId && BillingActions.Contains(a.Action)).OrderBy(a => a.OccurredAt).ToListAsync());

    public static Task<int> ReminderNoticeCountAsync(OkulApiFactory factory, Guid guardianUserId) =>
        QueryAsync(factory, c => c.GuardianNotices.IgnoreQueryFilters().CountAsync(n => n.GuardianUserId == guardianUserId && n.Kind == Domain.Communication.GuardianNoticeKind.PaymentReminder));
}
