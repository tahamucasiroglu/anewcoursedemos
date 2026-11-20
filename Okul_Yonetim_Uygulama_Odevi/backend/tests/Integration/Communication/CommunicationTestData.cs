using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Tests.Integration.Account;
using OkulYonetim.Tests.Integration.Attendance;
using OkulYonetim.Tests.Integration.Infrastructure;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Communication;

/// <summary>
/// Spec 0011 test yardımcıları: sabit saatli (varsayılan 2026-11-20 09:00 UTC = Türkiye 12:00, Cuma), bildirimi yakalayan
/// (ya da verilen göndericiyle) ve e-postayı yakalayan API; hazır okul (<see cref="CommunicationWorld"/>); duyuru, akış,
/// ayrıntı, sonuç, onay, konuşma, mesaj ve mesai saati uçları; veritabanı sayaçları.
/// </summary>
public static class CommunicationTestData
{
    public static OkulApiFactory Factory(TestDatabase database, IPushNotifier? notifier = null, TimeProvider? time = null) =>
        new(database.ConnectionString, timeProvider: time ?? new FakeTimeProvider(Morning), configureServices: services =>
        {
            services.RemoveAll<IEmailOutbox>();
            services.AddSingleton<IEmailOutbox>(new CapturingOutbox());
            if (notifier is not null)
            {
                services.RemoveAll<IPushNotifier>();
                services.AddSingleton(notifier);
            }
        });

    public static async Task<CommunicationWorld> WorldAsync(OkulApiFactory factory, HttpClient client)
    {
        AttendanceClass c = await ClassAsync(factory, client);
        Guid ali = await StudentAsync(client, c, "Ali Yılmaz");
        Guid ayse = await StudentAsync(client, c, "Ayşe Kaya");
        ParentAccount aliMother = await ParentAsync(client, c, ali, "Mother");
        ParentAccount aliFather = await ParentAsync(client, c, ali, "Father");
        ParentAccount ayseMother = await ParentAsync(client, c, ayse, "Mother");
        Guid other = await CreateClassroomAsync(client, c.Founder, c.TermId, c.School.BranchId, c.LevelId, "5-B");
        (Guid otherTeacherId, _, string otherTeacher) = await TeacherAsync(factory, client, c.School.TenantId, c.School.BranchId);
        await AssignAsync(client, c.Founder, other, otherTeacherId);
        Guid berk = await StudentAsync(client, c, "Berk Demir", other);
        ParentAccount berkMother = await ParentAsync(client, c, berk, "Mother");
        return new CommunicationWorld(c, ali, ayse, aliMother, aliFather, ayseMother, other, otherTeacherId, otherTeacher, berk, berkMother);
    }

    public static async Task<ParentAccount> ParentAsync(HttpClient client, AttendanceClass c, Guid studentId, string relationship = "Mother")
    {
        (string token, Guid userId, string email, string password) = await NewGuardianAsync(client, c.Founder, c.School.TenantId, studentId, relationship);
        return new ParentAccount(userId, email, token, password);
    }

    /// <summary>Sahte saat ileri alındıktan sonra (erişim anahtarı süresi dolar) aynı veliyle yeniden giriş.</summary>
    public static async Task<string> ReloginAsync(HttpClient client, AttendanceClass c, ParentAccount parent) =>
        await TokenForSchoolAsync(client, parent.Email, c.School.TenantId, parent.Password);

    public static object Plain(string scope, Guid? scopeId, string title = "Veli toplantısı", string body = "Cuma 14:00'te okulda.") =>
        new { title, body, scope, scopeId, requestConsent = false };

    public static object Form(string scope, Guid? scopeId, string deadline, string question = "Geziye katılsın mı?", string title = "Müze gezisi", string body = "Pazartesi müze gezisi var.") =>
        new { title, body, scope, scopeId, requestConsent = true, consentQuestion = question, consentDeadline = deadline };

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> PublishAsync(HttpClient client, string token, object body) =>
        CallAsync(client, HttpMethod.Post, "/api/announcements", token, body);

    public static async Task<Guid> PublishedAsync(HttpClient client, string token, object body)
    {
        var (status, json, raw) = await PublishAsync(client, token, body);
        if (status != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"duyuru yayınlanamadı: {status} {raw}");
        }

        return json.GetProperty("id").GetGuid();
    }

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> FeedAsync(HttpClient client, string token, int page = 1) =>
        CallAsync(client, HttpMethod.Get, $"/api/announcements/feed?page={page}", token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> DetailAsync(HttpClient client, string token, Guid id) =>
        CallAsync(client, HttpMethod.Get, $"/api/announcements/{id}", token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> ResultsAsync(HttpClient client, string token, Guid id) =>
        CallAsync(client, HttpMethod.Get, $"/api/announcements/{id}/results", token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> AnswerAsync(HttpClient client, string token, Guid id, Guid studentId, string answer) =>
        CallAsync(client, HttpMethod.Post, $"/api/announcements/{id}/consent", token, new { studentId, answer });

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> StartAsync(HttpClient client, string token, Guid studentId, Guid counterpartUserId) =>
        CallAsync(client, HttpMethod.Post, "/api/conversations", token, new { studentId, counterpartUserId });

    public static async Task<Guid> StartedAsync(HttpClient client, string token, Guid studentId, Guid counterpartUserId)
    {
        var (status, json, raw) = await StartAsync(client, token, studentId, counterpartUserId);
        if (status != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"konuşma açılamadı: {status} {raw}");
        }

        return json.GetProperty("id").GetGuid();
    }

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> ConversationsAsync(HttpClient client, string token) =>
        CallAsync(client, HttpMethod.Get, "/api/conversations", token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> MessagesAsync(HttpClient client, string token, Guid conversationId, Guid? before = null) =>
        CallAsync(client, HttpMethod.Get, $"/api/conversations/{conversationId}/messages" + (before is { } b ? $"?before={b}" : string.Empty), token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> SendAsync(HttpClient client, string token, Guid conversationId, string body) =>
        CallAsync(client, HttpMethod.Post, $"/api/conversations/{conversationId}/messages", token, new { body });

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> SetQuietAsync(HttpClient client, string token, string? start, string? end, params string[] days) =>
        CallAsync(client, HttpMethod.Put, "/api/me/quiet-hours", token, new { enabled = true, start, end, days });

    public static async Task<JsonElement> MeAsync(HttpClient client, string token) =>
        (await CallAsync(client, HttpMethod.Get, "/api/me", token)).Body;

    public static Task<int> AnnouncementCountAsync(OkulApiFactory factory, Guid tenantId) =>
        QueryAsync(factory, c => c.Announcements.IgnoreQueryFilters().CountAsync(a => a.TenantId == tenantId));

    public static Task<AnnouncementRecipient> RecipientAsync(OkulApiFactory factory, Guid announcementId, Guid guardianUserId) =>
        QueryAsync(factory, c => c.AnnouncementRecipients.IgnoreQueryFilters().AsNoTracking().SingleAsync(r => r.AnnouncementId == announcementId && r.GuardianUserId == guardianUserId));

    public static string[] Titles(JsonElement feed) => [.. feed.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("title").GetString()!)];
}
