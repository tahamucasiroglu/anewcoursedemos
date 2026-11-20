using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Identity;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Administration;

/// <summary>
/// Spec 0005 test yardımcıları: belirli okulu seçmiş erişim anahtarı (iki okullu kişide okul seçimi yapılır), yönetim
/// uçlarına çağrı (durum kodu + JSON gövde + ham metin) ve geçici şifre biçimi.
/// </summary>
public static class AdminTestData
{
    /// <summary>12 karakter, harf+rakam, 0/O/1/l/I yok (spec 0005 Ö-1).</summary>
    public const string TemporaryPasswordPattern = "^[a-km-zA-HJ-NP-Z2-9]{12}$";

    public static async Task<string> TokenForSchoolAsync(HttpClient client, string email, Guid tenantId, string password = Password)
    {
        JsonElement auth = await LoginAsync(client, email, password);
        if (!auth.GetProperty("requiresTenantSelection").GetBoolean())
        {
            return AccessToken(auth);
        }

        using HttpResponseMessage selected = await SendAuthorizedAsync(client, HttpMethod.Post, "/api/auth/select-tenant", AccessToken(auth), new { tenantId });
        selected.EnsureSuccessStatusCode();
        return AccessToken(await ReadJsonAsync(selected));
    }

    public static async Task<(HttpStatusCode Status, JsonElement Body, string Raw)> CallAsync(HttpClient client, HttpMethod method, string path, string token, object? body = null)
    {
        using HttpResponseMessage response = await SendAuthorizedAsync(client, method, path, token, body);
        string raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        JsonElement json = string.IsNullOrEmpty(raw) ? default : JsonDocument.Parse(raw).RootElement.Clone();
        return (response.StatusCode, json, raw);
    }

    public static object Teacher(params Guid[] branchIds) => new { role = "Teacher", branchIds };

    public static object Founder() => new { role = "Founder", branchIds = Array.Empty<Guid>() };

    public static object NewMember(string email, string fullName = "Test Öğretmen", string? phone = null, params object[] roles) =>
        new { fullName, email, phone, roles };

    public static async Task<Guid> CreateBranchAsync(HttpClient client, string token, string name)
    {
        var (status, body, raw) = await CallAsync(client, HttpMethod.Post, "/api/admin/branches", token, new { name });
        if (status != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"şube eklenemedi: {status} {raw}");
        }

        return body.GetProperty("id").GetGuid();
    }

    public static async Task<HttpStatusCode> StatusAsync(HttpClient client, HttpMethod method, string path, string token, object? body = null) =>
        (await CallAsync(client, method, path, token, body)).Status;

    public static string Detail(JsonElement problem) =>
        problem.ValueKind == JsonValueKind.Object && problem.TryGetProperty("detail", out JsonElement d) ? d.GetString() ?? string.Empty : string.Empty;

    public static Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string path, object body) =>
        client.PostAsJsonAsync(new Uri(path, UriKind.Relative), body, TestContext.Current.CancellationToken);
}
