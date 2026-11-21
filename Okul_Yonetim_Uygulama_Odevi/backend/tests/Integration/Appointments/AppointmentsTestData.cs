using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Appointments;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Tests.Integration.Account;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Appointments;

/// <summary>
/// Spec 0016 test yardımcıları: sabit saatli (varsayılan 2026-11-20 09:00 UTC = Türkiye 12:00, Cuma), bildirimi yakalayan (ya da
/// verilen göndericiyle) API ve isteğe bağlı servis değiştirme (eşzamanlılık kapısı, commit'te düşen iş birimi); 0011'in hazır okulu
/// (<see cref="CommunicationWorld"/>: 5-A + öğretmeni, Ali — annesi ve babası veli —, Ayşe — annesi veli —; 5-B + başka öğretmen, Berk);
/// randevu uçları ve veritabanı sayaçları. Görüşme günü <see cref="Day"/> = 26.11.2026 (Türkiye 14:00 = 11:00 UTC).
/// </summary>
public static class AppointmentsTestData
{
    public const string Day = "2026-11-26";
    public static readonly DateTimeOffset At1400 = new(2026, 11, 26, 11, 0, 0, TimeSpan.Zero);

    public static OkulApiFactory AppointmentsFactory(TestDatabase database, IPushNotifier? notifier = null, TimeProvider? time = null, Action<IServiceCollection>? configure = null) =>
        new(database.ConnectionString, timeProvider: time ?? new FakeTimeProvider(Morning), configureServices: services =>
        {
            services.RemoveAll<IEmailOutbox>();
            services.AddSingleton<IEmailOutbox>(new CapturingOutbox());
            if (notifier is not null)
            {
                services.RemoveAll<IPushNotifier>();
                services.AddSingleton(notifier);
            }

            configure?.Invoke(services);
        });

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> OpenAsync(HttpClient client, string token, string? day = Day, string? start = "14:00", string? end = "16:00", int? durationMinutes = 20) =>
        CallAsync(client, HttpMethod.Post, "/api/appointments/availability", token, new { day, start, end, durationMinutes });

    public static async Task<int> OpenedAsync(HttpClient client, string token, string day = Day, string start = "14:00", string end = "16:00", int durationMinutes = 20)
    {
        var (status, body, raw) = await OpenAsync(client, token, day, start, end, durationMinutes);
        if (status != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"müsaitlik açılamadı: {status} {raw}");
        }

        return body.GetProperty("slotCount").GetInt32();
    }

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> ScheduleAsync(HttpClient client, string token) =>
        CallAsync(client, HttpMethod.Get, "/api/appointments/schedule", token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> RemoveSlotAsync(HttpClient client, string token, Guid slotId) =>
        CallAsync(client, HttpMethod.Delete, $"/api/appointments/slots/{slotId}", token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> TeachersAsync(HttpClient client, string token, Guid studentId) =>
        CallAsync(client, HttpMethod.Get, $"/api/appointments/teachers?studentId={studentId}", token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> SlotsAsync(HttpClient client, string token, Guid teacherId, Guid studentId) =>
        CallAsync(client, HttpMethod.Get, $"/api/appointments/teachers/{teacherId}/slots?studentId={studentId}", token);

    /// <summary>Velinin gördüğü boş dilimlerin kimlikleri (saat sırasıyla).</summary>
    public static async Task<Guid[]> FreeSlotIdsAsync(HttpClient client, string token, Guid teacherId, Guid studentId)
    {
        var (status, body, raw) = await SlotsAsync(client, token, teacherId, studentId);
        return status == HttpStatusCode.OK
            ? [.. body.EnumerateArray().Select(s => s.GetProperty("slotId").GetGuid())]
            : throw new InvalidOperationException($"boş saatler okunamadı: {status} {raw}");
    }

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> RequestAsync(HttpClient client, string token, Guid slotId, Guid studentId, string? topic = "Matematik notları") =>
        CallAsync(client, HttpMethod.Post, "/api/appointments", token, new { slotId, studentId, topic });

    public static async Task<Guid> RequestedAsync(HttpClient client, string token, Guid slotId, Guid studentId, string topic = "Matematik notları")
    {
        var (status, body, raw) = await RequestAsync(client, token, slotId, studentId, topic);
        return status == HttpStatusCode.OK
            ? body.GetProperty("appointmentId").GetGuid()
            : throw new InvalidOperationException($"randevu talep edilemedi: {status} {raw}");
    }

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> RespondAsync(HttpClient client, string token, Guid appointmentId, string decision, string? reason = null) =>
        CallAsync(client, HttpMethod.Post, $"/api/appointments/{appointmentId}/respond", token, new { decision, reason });

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> CancelAsync(HttpClient client, string token, Guid appointmentId, string? reason = null) =>
        CallAsync(client, HttpMethod.Post, $"/api/appointments/{appointmentId}/cancel", token, new { reason });

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> MineAsync(HttpClient client, string token) =>
        CallAsync(client, HttpMethod.Get, "/api/appointments/mine", token);

    /// <summary>Takvimdeki dilimin durumu (yoksa <c>null</c>).</summary>
    public static string? StateOf(JsonElement schedule, Guid slotId) =>
        schedule.GetProperty("slots").EnumerateArray().Where(s => s.GetProperty("slotId").GetGuid() == slotId).Select(s => s.GetProperty("state").GetString()).FirstOrDefault();

    public static JsonElement[] Rows(JsonElement mine, string list) => [.. mine.GetProperty(list).EnumerateArray()];

    public static Task<List<Appointment>> AppointmentRowsAsync(OkulApiFactory factory, Guid tenantId) =>
        QueryAsync(factory, c => c.Appointments.IgnoreQueryFilters().AsNoTracking().Where(a => a.TenantId == tenantId).OrderBy(a => a.RequestedAt).ToListAsync());

    public static Task<List<AvailabilitySlot>> SlotRowsAsync(OkulApiFactory factory, Guid tenantId) =>
        QueryAsync(factory, c => c.AvailabilitySlots.IgnoreQueryFilters().AsNoTracking().Where(s => s.TenantId == tenantId).OrderBy(s => s.StartsAt).ToListAsync());

    public static Task<List<GuardianNotice>> AppointmentNoticesAsync(OkulApiFactory factory, Guid studentId) =>
        QueryAsync(factory, c => c.GuardianNotices.IgnoreQueryFilters().AsNoTracking().Where(n => n.StudentId == studentId && n.Kind == GuardianNoticeKind.Appointment).OrderBy(n => n.CreatedAt).ToListAsync());
}
