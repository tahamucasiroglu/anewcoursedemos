using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Tests.Integration.Billing;
using OkulYonetim.Tests.Integration.Infrastructure;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Dashboard;

/// <summary>
/// Spec 0017 test yardımcıları: API fabrikası 0015'in <c>BillingFactory</c>'si (sabit ya da ileri alınabilen sahte saat; varsayılan 2026-11-20
/// 09:00 UTC = Türkiye 12:00, Cuma), hazır okul <see cref="BillingWorld"/> (Merkez: 5-A — Ali, Ayşe —, 5-B — Berk —; Kadıköy: sınıfsız Cem;
/// muhasebe; tarife 10.000 TL), panelin dokuz ucu, dışa aktarılan dosyanın satırları ve audit kayıtları.
/// </summary>
public static class DashboardTestData
{

    /// <summary>Kartlar, raporlar ve dışa aktarmalar — yetki testleri hepsini dolaşır.</summary>
    public static readonly string[] AllPaths =
    [
        "/api/dashboard/summary/attendance",
        "/api/dashboard/summary/overdue",
        "/api/dashboard/summary/announcements",
        "/api/dashboard/reports/attendance?startDate=2026-11-01&endDate=2026-11-20",
        "/api/dashboard/reports/payments?startDate=2026-11-01&endDate=2026-11-20",
        "/api/dashboard/reports/balances",
        "/api/dashboard/reports/attendance/export?startDate=2026-11-01&endDate=2026-11-20",
        "/api/dashboard/reports/payments/export?startDate=2026-11-01&endDate=2026-11-20",
        "/api/dashboard/reports/balances/export",
    ];

    public static OkulApiFactory DashboardFactory(TestDatabase database, TimeProvider? time = null) => BillingTestData.BillingFactory(database, time: time);

    public static FakeTimeProvider Clock(DateTimeOffset? start = null) => new(start ?? Attendance.AttendanceTestData.Morning);

    public static Task<BillingWorld> DashboardWorldAsync(OkulApiFactory factory, HttpClient client) => BillingTestData.BillingWorldAsync(factory, client);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> AttendanceCardAsync(HttpClient client, string token, Guid? branchId = null) =>
        CallAsync(client, HttpMethod.Get, "/api/dashboard/summary/attendance" + Branch(branchId, "?"), token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> OverdueCardAsync(HttpClient client, string token, Guid? branchId = null) =>
        CallAsync(client, HttpMethod.Get, "/api/dashboard/summary/overdue" + Branch(branchId, "?"), token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> AnnouncementsCardAsync(HttpClient client, string token, Guid? branchId = null) =>
        CallAsync(client, HttpMethod.Get, "/api/dashboard/summary/announcements" + Branch(branchId, "?"), token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> AttendanceReportAsync(HttpClient client, string token, string query) =>
        CallAsync(client, HttpMethod.Get, "/api/dashboard/reports/attendance?" + query, token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> PaymentsReportAsync(HttpClient client, string token, string query) =>
        CallAsync(client, HttpMethod.Get, "/api/dashboard/reports/payments?" + query, token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> BalancesReportAsync(HttpClient client, string token, string query = "") =>
        CallAsync(client, HttpMethod.Get, "/api/dashboard/reports/balances?" + query, token);

    /// <summary>Dışa aktarma: durum, gövde baytları, içerik türü, ek dosya adı ve (hata ise) gövde metni.</summary>
    public static async Task<ExportResponse> ExportAsync(HttpClient client, string token, string kind, string query = "")
    {
        using HttpRequestMessage request = new(HttpMethod.Get, new Uri($"/api/dashboard/reports/{kind}/export?{query}", UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        byte[] bytes = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        ContentDispositionHeaderValue? disposition = response.Content.Headers.ContentDisposition;
        return new ExportResponse(
            response.StatusCode,
            bytes,
            response.Content.Headers.ContentType?.ToString(),
            disposition?.FileName?.Trim('"'),
            Encoding.UTF8.GetString(bytes));
    }

    /// <summary>Dosyanın satırları (BOM'dan sonra, CRLF ile bölünmüş; son boş satır atılır) ve hücreleri (tırnaksız basit bölme — tırnaklı hücre testleri ham metne bakar).</summary>
    public static string[] Lines(byte[] file)
    {
        string text = Encoding.UTF8.GetString(file, 3, file.Length - 3);
        string[] lines = text.Split("\r\n");
        return lines[^1].Length == 0 ? lines[..^1] : lines;
    }

    public static Task<List<AuditEntry>> ExportAuditAsync(OkulApiFactory factory, Guid tenantId) =>
        QueryAsync(factory, c => c.AuditEntries.Where(a => a.TenantId == tenantId && a.Action == AuditAction.ReportExported).OrderBy(a => a.OccurredAt).ToListAsync());

    public static string[] Names(JsonElement items, string property = "fullName") =>
        [.. items.EnumerateArray().Select(i => i.GetProperty(property).GetString()!)];

    private static string Branch(Guid? branchId, string prefix) => branchId is { } id ? $"{prefix}branchId={id}" : string.Empty;
}
