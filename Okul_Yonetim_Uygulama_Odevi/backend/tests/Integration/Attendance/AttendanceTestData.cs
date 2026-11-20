using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Account;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Attendance;

/// <summary>
/// Spec 0010 test yardımcıları: sabit saatli (varsayılan 2026-11-20 09:00 UTC = Türkiye 12:00, Cuma) ve e-postayı yakalayan
/// API, hazır sınıf (dönem 2026-09-01 – 2027-01-22), sınıfa öğrenci ve veli ekleme, öğretmen atama, yoklama okuma/kaydetme ve
/// bildirim uçları, audit satırları.
/// </summary>
public static class AttendanceTestData
{
    public const string Today = "2026-11-20";
    public static readonly DateTimeOffset Morning = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);

    public static OkulApiFactory Factory(TestDatabase database, CapturingOutbox outbox, TimeProvider? time = null, Action<IServiceCollection>? configure = null) =>
        new(database.ConnectionString, timeProvider: time ?? new FakeTimeProvider(Morning), configureServices: services =>
        {
            services.RemoveAll<IEmailOutbox>();
            services.AddSingleton<IEmailOutbox>(outbox);
            configure?.Invoke(services);
        });

    /// <summary>Okul, dönem, seviye, "5-A" sınıfı ve atanmış öğretmen.</summary>
    public static async Task<AttendanceClass> ClassAsync(OkulApiFactory factory, HttpClient client, string termStart = "2026-09-01", string termEnd = "2027-01-22")
    {
        (SchoolSeed school, string founder) = await SchoolAsync(factory, client);
        Guid level = await CreateGradeLevelAsync(client, founder, "5. sınıf", 5);
        Guid term = await CreateTermAsync(client, founder, "Güz", termStart, termEnd);
        Guid classroom = await CreateClassroomAsync(client, founder, term, school.BranchId, level, "5-A");
        (Guid teacherId, string email, string token) = await TeacherAsync(factory, client, school.TenantId, school.BranchId);
        await AssignAsync(client, founder, classroom, teacherId);
        return new AttendanceClass(school, founder, term, level, classroom, teacherId, email, token);
    }

    /// <summary>Şubede Öğretmen rolü olan yeni kişi ve bu okulu seçmiş oturumu.</summary>
    public static async Task<(Guid UserId, string Email, string Token)> TeacherAsync(OkulApiFactory factory, HttpClient client, Guid tenantId, Guid branchId)
    {
        string email = UniqueEmail("ogretmen");
        Guid id = await AddMemberAsync(factory, email, tenantId, Role.Teacher, branchId);
        return (id, email, await TokenForSchoolAsync(client, email, tenantId));
    }

    public static async Task AssignAsync(HttpClient client, string founder, Guid classroomId, params Guid[] teacherUserIds)
    {
        var (status, _, raw) = await CallAsync(client, HttpMethod.Put, $"/api/admin/classrooms/{classroomId}/teachers", founder, new { teacherUserIds });
        if (status != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"öğretmen atanamadı: {status} {raw}");
        }
    }

    public static Task<Guid> StudentAsync(HttpClient client, AttendanceClass c, string fullName, Guid? classroomId = null) =>
        CreateStudentAsync(client, c.Founder, NewStudent(c.School.BranchId, fullName, classroomId: classroomId ?? c.ClassroomId));

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> TodayAsync(HttpClient client, string token, Guid classroomId) =>
        CallAsync(client, HttpMethod.Get, $"/api/attendance/classrooms/{classroomId}/today", token);

    /// <summary>
    /// Kaydetme. <paramref name="version"/> ekranın açıldığı yoklama sürümüdür; verilmezse ekran şimdi açılmış sayılır (önce
    /// bugünkü liste okunur, okunamazsa 0) — eski ekranı sınayan testler sürümü açıkça verir (0010 review F-1).
    /// </summary>
    public static async Task<(HttpStatusCode Status, JsonElement Body, string Raw)> SaveAsync(HttpClient client, string token, Guid classroomId, IEnumerable<(Guid StudentId, string Status)> entries, string day = Today, int? version = null)
    {
        if (version is null)
        {
            var (status, body, _) = await TodayAsync(client, token, classroomId);
            version = status == HttpStatusCode.OK ? body.GetProperty("version").GetInt32() : 0;
        }

        return await CallAsync(client, HttpMethod.Put, $"/api/attendance/classrooms/{classroomId}/today", token, new
        {
            day,
            entries = entries.Select(e => new { studentId = e.StudentId, status = e.Status }).ToArray(),
            version,
        });
    }

    /// <summary>Sınıfın bütün öğrencileri "Present"; verilenler kendi durumlarıyla.</summary>
    public static IEnumerable<(Guid StudentId, string Status)> Marks(IEnumerable<Guid> all, params (Guid StudentId, string Status)[] overrides) =>
        all.Select(id => (id, overrides.FirstOrDefault(o => o.StudentId == id).Status ?? "Present"));

    public static async Task<JsonElement> SavedAsync(HttpClient client, string token, Guid classroomId, IEnumerable<(Guid StudentId, string Status)> entries)
    {
        var (status, body, raw) = await SaveAsync(client, token, classroomId, entries);
        if (status != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"yoklama kaydedilemedi: {status} {raw}");
        }

        return body;
    }

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> NoticesAsync(HttpClient client, string token) =>
        CallAsync(client, HttpMethod.Get, "/api/notices", token);

    public static async Task<int> UnreadAsync(HttpClient client, string token) =>
        (await CallAsync(client, HttpMethod.Get, "/api/me", token)).Body.GetProperty("unreadNoticeCount").GetInt32();

    public static Task<List<AuditEntry>> AuditAsync(OkulApiFactory factory, Guid tenantId, params AuditAction[] actions) =>
        QueryAsync(factory, c => c.AuditEntries.Where(e => e.TenantId == tenantId && actions.Contains(e.Action)).OrderBy(e => e.Id).ToListAsync());

    public static Task<int> SheetCountAsync(OkulApiFactory factory, Guid classroomId) =>
        QueryAsync(factory, c => c.AttendanceSheets.IgnoreQueryFilters().CountAsync(s => s.ClassroomId == classroomId));

    public static Task<int> NoticeCountAsync(OkulApiFactory factory, Guid studentId) =>
        QueryAsync(factory, c => c.GuardianNotices.IgnoreQueryFilters().CountAsync(n => n.StudentId == studentId));
}
