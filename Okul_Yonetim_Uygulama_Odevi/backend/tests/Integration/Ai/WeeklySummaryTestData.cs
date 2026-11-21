using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Ai;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Tests.Integration.Account;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Spec 0014 test yardımcıları: sahte metin servisli (verilmezse Testing ortamındaki "kullanılamıyor" kaydı), bildirimi yakalayan,
/// sahte saatli API (varsayılan 2026-11-20 09:00 UTC = Türkiye 12:00 Cuma → bu hafta 16–22 Kasım, geçen hafta 9–15 Kasım; saat
/// testte ileri alınabilir); hazır okul 0011'in <see cref="CommunicationWorld"/>'ü (5-A'da Ali ve Ayşe, 5-B'de Berk); haftalık özet
/// uçları ve veritabanındaki özet taslakları ile özet bildirimleri.
/// </summary>
public static class WeeklySummaryTestData
{
    public const string ThisWeekLabel = "16–22 Kasım";
    public const string Label = "Yapay zekâ desteğiyle hazırlandı, öğretmen onayladı.";
    public const string Unavailable = "Yapay zekâ şu an kullanılamıyor. Daha sonra tekrar deneyin.";
    public const string NoData = "Bu hafta için özetlenecek veri yok.";
    public const string AlreadySent = "Bu hafta için özet zaten gönderildi.";
    public const string NoGuardian = "Bu öğrencinin kayıtlı velisi yok.";
    public const string DraftChanged = "Taslak değişti; paneli yenileyin.";

    public static OkulApiFactory Factory(TestDatabase database, IAiTextGenerator? text, CapturingPushNotifier? notifier = null, FakeTimeProvider? time = null, Action<IServiceCollection>? configure = null) =>
        new(database.ConnectionString, timeProvider: time ?? new FakeTimeProvider(Morning), configureServices: services =>
        {
            services.RemoveAll<IEmailOutbox>();
            services.AddSingleton<IEmailOutbox>(new CapturingOutbox());
            services.RemoveAll<IPushNotifier>();
            services.AddSingleton<IPushNotifier>(notifier ?? new CapturingPushNotifier());
            if (text is not null)
            {
                services.RemoveAll<IAiTextGenerator>();
                services.AddSingleton(text);
            }

            configure?.Invoke(services);
        });

    public static string WeekQuery(string? week) => week is null ? string.Empty : "?week=" + week;

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> ClassroomsAsync(HttpClient client, string token) =>
        CallAsync(client, HttpMethod.Get, "/api/weekly-summaries/classrooms", token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> ListAsync(HttpClient client, string token, Guid classroomId, string? week = null) =>
        CallAsync(client, HttpMethod.Get, $"/api/weekly-summaries?classroomId={classroomId}" + (week is null ? string.Empty : "&week=" + week), token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> PanelAsync(HttpClient client, string token, Guid studentId, string? week = null) =>
        CallAsync(client, HttpMethod.Get, $"/api/weekly-summaries/students/{studentId}" + WeekQuery(week), token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> GenerateAsync(HttpClient client, string token, Guid studentId, string? teacherNote = null, string? week = null) =>
        CallAsync(client, HttpMethod.Post, $"/api/weekly-summaries/students/{studentId}/draft" + WeekQuery(week), token, new { teacherNote });

    public static async Task<JsonElement> GeneratedAsync(HttpClient client, string token, Guid studentId, string? teacherNote = null, string? week = null)
    {
        var (status, body, raw) = await GenerateAsync(client, token, studentId, teacherNote, week);
        if (status != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"özet taslağı oluşmadı: {status} {raw}");
        }

        return body;
    }

    // "Onayla ve gönder". Taslak verilmezse web paneli gibi önce paneli açar ve oradaki bekleyen taslağı (kimlik + oluşturulma zamanı)
    // gönderir (review R1-2); panel açılmazsa ya da bekleyen taslak yoksa var olmayan bir taslak gönderilir (sunucu kendi hatasını verir).
    public static async Task<(HttpStatusCode Status, JsonElement Body, string Raw)> SendSummaryAsync(HttpClient client, string token, Guid studentId, string? text, string? week = null, Guid? draftId = null, DateTimeOffset? draftGeneratedAt = null)
    {
        if (draftId is null)
        {
            var (panelStatus, panel, _) = await PanelAsync(client, token, studentId, week);
            bool pending = panelStatus == HttpStatusCode.OK && panel.GetProperty("draftId").ValueKind == JsonValueKind.String;
            draftId = pending ? panel.GetProperty("draftId").GetGuid() : Guid.NewGuid();
            draftGeneratedAt = pending ? panel.GetProperty("draftGeneratedAt").GetDateTimeOffset() : DateTimeOffset.UnixEpoch;
        }

        return await CallAsync(client, HttpMethod.Post, $"/api/weekly-summaries/students/{studentId}/send" + WeekQuery(week), token, new { text, draftId, draftGeneratedAt });
    }

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> DiscardSummaryAsync(HttpClient client, string token, Guid studentId, string? week = null) =>
        CallAsync(client, HttpMethod.Delete, $"/api/weekly-summaries/students/{studentId}/draft" + WeekQuery(week), token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> NoticeAsync(HttpClient client, string token, Guid noticeId) =>
        CallAsync(client, HttpMethod.Get, $"/api/notices/{noticeId}", token);

    /// <summary>Bugünkü yoklamayı kaydeder (verilmeyenler "Var").</summary>
    public static Task<JsonElement> AttendAsync(HttpClient client, CommunicationWorld w, params (Guid StudentId, string Status)[] marks) =>
        SavedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Marks([w.Ali, w.Ayse], marks));

    public static Task<List<AiDraft>> SummaryDraftsAsync(OkulApiFactory factory, Guid tenantId) =>
        QueryAsync(factory, c => c.AiDrafts.IgnoreQueryFilters().AsNoTracking().Where(d => d.TenantId == tenantId && d.Kind == AiDraftKind.WeeklySummary).ToListAsync());

    public static Task<List<GuardianNotice>> SummaryNoticesAsync(OkulApiFactory factory, Guid tenantId) =>
        QueryAsync(factory, c => c.GuardianNotices.IgnoreQueryFilters().AsNoTracking().Where(n => n.TenantId == tenantId && n.Kind == GuardianNoticeKind.WeeklySummary).ToListAsync());

    public static string[] Statuses(JsonElement list) =>
        [.. list.GetProperty("students").EnumerateArray().Select(s => s.GetProperty("status").GetString()!)];

    public static Dictionary<Guid, string> StatusByStudent(JsonElement list) =>
        list.GetProperty("students").EnumerateArray().ToDictionary(s => s.GetProperty("studentId").GetGuid(), s => s.GetProperty("status").GetString()!);
}
