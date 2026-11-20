using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Domain.Students;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Students;

/// <summary>
/// Spec 0009 test yardımcıları: sabit saatli API (bugün 2026-11-20, Türkiye günü), kurucu oturumuyla okul, öğrenci ve veli
/// gövdeleri, öğrenci ekleme, veli bağlama ve velinin okul oturumu; veritabanındaki öğrenci satırları (filtre dışı — yalnızca
/// testte, şifreli alanları doğrulamak için).
/// </summary>
public static class StudentsTestData
{
    public static readonly DateTimeOffset Now = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);

    public static OkulApiFactory Factory(TestDatabase database, string? personalDataKey = OkulApiFactory.TestPersonalDataKey) =>
        new(database.ConnectionString, timeProvider: new FakeTimeProvider(Now), personalDataKey: personalDataKey);

    public static async Task<(SchoolSeed School, string Founder)> SchoolAsync(OkulApiFactory factory, HttpClient client)
    {
        SchoolSeed school = await CreateSchoolAsync(factory);
        return (school, await TokenForSchoolAsync(client, school.FounderEmail, school.TenantId));
    }

    public static Dictionary<string, object?> NewStudent(
        Guid branchId,
        string fullName = "Ali Yılmaz",
        string birthDate = "2015-03-01",
        string? schoolNumber = null,
        Guid? classroomId = null,
        string? healthNote = null,
        string? allergy = null) =>
        new()
        {
            ["fullName"] = fullName,
            ["birthDate"] = birthDate,
            ["schoolNumber"] = schoolNumber,
            ["branchId"] = branchId,
            ["classroomId"] = classroomId,
            ["healthNote"] = healthNote,
            ["allergy"] = allergy,
        };

    public static async Task<Guid> CreateStudentAsync(HttpClient client, string token, object body)
    {
        var (status, json, raw) = await CallAsync(client, HttpMethod.Post, "/api/admin/students", token, body);
        if (status != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"öğrenci eklenemedi: {status} {raw}");
        }

        return json.GetProperty("id").GetGuid();
    }

    public static object Guardian(string email, string fullName = "Veli Kişi", string? relationship = "Mother", string? phone = null) =>
        new { fullName, email, phone, relationship };

    public static async Task<JsonElement> LinkAsync(HttpClient client, string token, Guid studentId, object body)
    {
        var (status, json, raw) = await CallAsync(client, HttpMethod.Post, $"/api/admin/students/{studentId}/guardians", token, body);
        if (status != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"veli bağlanamadı: {status} {raw}");
        }

        return json;
    }

    /// <summary>Yeni veliyi bağlar; geçici şifresiyle bu okulu seçmiş oturumunu ve kişi kimliğini döndürür.</summary>
    public static async Task<(string Token, Guid UserId, string Email, string Password)> NewGuardianAsync(HttpClient client, string founder, Guid tenantId, Guid studentId, string relationship = "Mother")
    {
        string email = UniqueEmail("veli");
        JsonElement result = await LinkAsync(client, founder, studentId, Guardian(email, relationship: relationship));
        string password = result.GetProperty("temporaryPassword").GetString()!;
        return (await TokenForSchoolAsync(client, email, tenantId, password), result.GetProperty("userId").GetGuid(), email, password);
    }

    public static async Task<JsonElement> CardAsync(HttpClient client, string token, Guid studentId)
    {
        var (status, json, raw) = await CallAsync(client, HttpMethod.Get, $"/api/admin/students/{studentId}", token);
        if (status != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"kart okunamadı: {status} {raw}");
        }

        return json;
    }

    public static async Task<JsonElement> ListAsync(HttpClient client, string token, string query = "")
    {
        var (status, json, raw) = await CallAsync(client, HttpMethod.Get, "/api/admin/students" + query, token);
        if (status != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"liste okunamadı: {status} {raw}");
        }

        return json;
    }

    public static Guid[] Ids(JsonElement items) => [.. items.EnumerateArray().Select(x => x.GetProperty("id").GetGuid())];

    public static Task<List<Student>> StudentRowsAsync(OkulApiFactory factory, Guid tenantId) =>
        QueryAsync(factory, c => c.Students.IgnoreQueryFilters().AsNoTracking().Where(s => s.TenantId == tenantId).ToListAsync());
}
