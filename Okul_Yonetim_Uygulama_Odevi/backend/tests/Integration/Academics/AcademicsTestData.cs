using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Academics;

/// <summary>
/// Spec 0013 test yardımcıları (API fabrikası 0011'in <c>CommunicationTestData.Factory</c>'si: sabit saat 2026-11-20 09:00 UTC =
/// Türkiye 12:00, Cuma; istenirse bildirimi yakalayan gönderici), hazır okul (0011'in <see cref="CommunicationWorld"/>'ü: 5-A'da Ali ve Ayşe, 5-B'de Berk), ödev/teslim/değerlendirme/puan
/// ve veli özeti uçları, veritabanı sayaçları.
/// </summary>
public static class AcademicsTestData
{
    public const string Tomorrow = "2026-11-21";

    public static object Homework(string subject = "Matematik", string title = "Kesirler alıştırması", string? description = "Sayfa 42, 1-10", string dueDate = "2026-11-25") =>
        new { subject, title, description, dueDate };

    public static object Exam(string subject = "Türkçe", string kind = "Written", string title = "1. yazılı", string date = Attendance.AttendanceTestData.Today) =>
        new { subject, kind, title, date };

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> CreateAssignmentAsync(HttpClient client, string token, Guid classroomId, object body) =>
        CallAsync(client, HttpMethod.Post, $"/api/academics/classrooms/{classroomId}/assignments", token, body);

    public static async Task<Guid> AssignedAsync(HttpClient client, string token, Guid classroomId, object? body = null)
    {
        var (status, json, raw) = await CreateAssignmentAsync(client, token, classroomId, body ?? Homework());
        if (status != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"ödev verilemedi: {status} {raw}");
        }

        return json.GetProperty("id").GetGuid();
    }

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> AssignmentsAsync(HttpClient client, string token, Guid classroomId) =>
        CallAsync(client, HttpMethod.Get, $"/api/academics/classrooms/{classroomId}/assignments", token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> SubmissionsAsync(HttpClient client, string token, Guid assignmentId) =>
        CallAsync(client, HttpMethod.Get, $"/api/academics/assignments/{assignmentId}/submissions", token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> SaveSubmissionsAsync(HttpClient client, string token, Guid assignmentId, params (Guid StudentId, string Status)[] entries) =>
        CallAsync(client, HttpMethod.Put, $"/api/academics/assignments/{assignmentId}/submissions", token, new
        {
            entries = entries.Select(e => new { studentId = e.StudentId, status = e.Status }).ToArray(),
        });

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> CreateAssessmentAsync(HttpClient client, string token, Guid classroomId, object body) =>
        CallAsync(client, HttpMethod.Post, $"/api/academics/classrooms/{classroomId}/assessments", token, body);

    public static async Task<Guid> OpenedAsync(HttpClient client, string token, Guid classroomId, object? body = null)
    {
        var (status, json, raw) = await CreateAssessmentAsync(client, token, classroomId, body ?? Exam());
        if (status != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"değerlendirme açılamadı: {status} {raw}");
        }

        return json.GetProperty("id").GetGuid();
    }

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> AssessmentsAsync(HttpClient client, string token, Guid classroomId) =>
        CallAsync(client, HttpMethod.Get, $"/api/academics/classrooms/{classroomId}/assessments", token);

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> GradesAsync(HttpClient client, string token, Guid assessmentId) =>
        CallAsync(client, HttpMethod.Get, $"/api/academics/assessments/{assessmentId}/grades", token);

    /// <summary>Puan kaydı; puan JSON'a olduğu gibi gider (sayı, metin ya da <c>null</c>).</summary>
    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> SaveGradesAsync(HttpClient client, string token, Guid assessmentId, params (Guid StudentId, object? Score)[] grades) =>
        CallAsync(client, HttpMethod.Put, $"/api/academics/assessments/{assessmentId}/grades", token, new
        {
            grades = grades.Select(g => new { studentId = g.StudentId, score = g.Score }).ToArray(),
        });

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> SummaryAsync(HttpClient client, string token, Guid studentId) =>
        CallAsync(client, HttpMethod.Get, $"/api/my/children/{studentId}/academics", token);

    /// <summary>Listedeki öğrencilerin (kimlik → durum/puan metni) eşlemesi.</summary>
    public static Dictionary<Guid, string?> ByStudent(JsonElement view, string field) =>
        view.GetProperty("students").EnumerateArray().ToDictionary(
            s => s.GetProperty("studentId").GetGuid(),
            s => s.GetProperty(field).ValueKind == JsonValueKind.Null ? null : s.GetProperty(field).ToString());

    public static string[] Names(JsonElement view) =>
        [.. view.GetProperty("students").EnumerateArray().Select(s => s.GetProperty("fullName").GetString()!)];

    public static string[] RowTitles(JsonElement items) =>
        [.. items.EnumerateArray().Select(i => i.GetProperty("title").GetString()!)];

    public static Task<int> AssignmentCountAsync(OkulApiFactory factory, Guid classroomId) =>
        QueryAsync(factory, c => c.Assignments.IgnoreQueryFilters().CountAsync(a => a.ClassroomId == classroomId));

    public static Task<int> AssessmentCountAsync(OkulApiFactory factory, Guid classroomId) =>
        QueryAsync(factory, c => c.Assessments.IgnoreQueryFilters().CountAsync(a => a.ClassroomId == classroomId));

    /// <summary>Öğrenciyi başka sınıfa alır (sınıftan ayrılma; kayıt silinmez).</summary>
    public static async Task MoveAsync(HttpClient client, string founder, Guid studentId, string fullName, Guid branchId, Guid? classroomId)
    {
        var (status, _, raw) = await CallAsync(client, HttpMethod.Put, $"/api/admin/students/{studentId}", founder, new { fullName, birthDate = "2015-03-01", branchId, classroomId });
        if (status != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"öğrenci taşınamadı: {status} {raw}");
        }
    }
}
