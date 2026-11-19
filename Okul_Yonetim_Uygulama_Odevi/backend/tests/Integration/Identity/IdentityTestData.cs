using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Identity.Commands.CreateSchool;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Persistence.Contexts;
using OkulYonetim.Tests.Integration.Infrastructure;

namespace OkulYonetim.Tests.Integration.Identity;

/// <summary>
/// Spec 0004 testlerinin ortak verisi ve yardımcıları: okul açma (uygulamanın kendi komutuyla), test verisiyle şube ve
/// şube düzeyinde kişi ekleme (yönetim ekranı spec 0005'te), giriş ve yetkili istemci. Her test benzersiz e-posta ve
/// okul adı kullanır (testler aynı veritabanını paylaşır).
/// </summary>
public static class IdentityTestData
{
    public const string Password = "Deneme-Sifre-2026";

    public static string UniqueEmail(string prefix = "kisi") => $"{prefix}-{Guid.NewGuid():N}@ornek.test";

    public static async Task<SchoolSeed> CreateSchoolAsync(OkulApiFactory factory, string? email = null, string password = Password, string? schoolName = null)
    {
        string founderEmail = email ?? UniqueEmail("kurucu");
        string school = schoolName ?? $"Okul {Guid.NewGuid():N}";
        using IServiceScope scope = factory.Services.CreateScope();
        CreateSchoolResult result = await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new CreateSchoolCommand(school, "Merkez", "Ayşe Kurucu", founderEmail, password), TestContext.Current.CancellationToken);
        return new SchoolSeed(result.TenantId, result.BranchId!.Value, result.UserId, founderEmail, school);
    }

    public static async Task<Guid> AddBranchAsync(OkulApiFactory factory, Guid tenantId, string name)
    {
        Branch branch = Branch.Create(tenantId, name, DateTimeOffset.UtcNow);
        await WithContextAsync(factory, context =>
        {
            context.Branches.Add(branch);
            return Task.CompletedTask;
        });
        return branch.Id;
    }

    /// <summary>Şube düzeyinde rolü olan kişi ekler (yoksa açar) — yönetim ekranı gelene kadar test verisi.</summary>
    public static async Task<Guid> AddMemberAsync(OkulApiFactory factory, string email, Guid tenantId, Role role, Guid? branchId, string password = Password)
    {
        Guid userId = Guid.Empty;
        await WithContextAsync(factory, async context =>
        {
            string normalized = EmailAddress.Normalize(email);
            User? user = await context.Users.FirstOrDefaultAsync(u => u.Email == normalized);
            if (user is null)
            {
                IPasswordHasher hasher = factory.Services.GetRequiredService<IPasswordHasher>();
                user = User.Create(email, "Test Kişi", hasher.Hash(password), DateTimeOffset.UtcNow);
                context.Users.Add(user);
            }

            userId = user.Id;
            context.RoleAssignments.Add(role == Role.Founder
                ? RoleAssignment.Founder(user.Id, tenantId, DateTimeOffset.UtcNow)
                : RoleAssignment.ForBranch(user.Id, tenantId, role, branchId!.Value, DateTimeOffset.UtcNow));
        });
        return userId;
    }

    public static async Task WithContextAsync(OkulApiFactory factory, Func<OkulDbContext, Task> action)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        OkulDbContext context = scope.ServiceProvider.GetRequiredService<OkulDbContext>();
        await action(context);
        await context.SaveChangesAsync();
    }

    public static async Task<T> QueryAsync<T>(OkulApiFactory factory, Func<OkulDbContext, Task<T>> query)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<OkulDbContext>());
    }

    public static Task<HttpResponseMessage> PostLoginAsync(HttpClient client, string email, string password, bool refreshTokenInBody = true) =>
        client.PostAsJsonAsync(new Uri("/api/auth/login", UriKind.Relative), new { email, password, refreshTokenInBody }, TestContext.Current.CancellationToken);

    public static async Task<JsonElement> LoginAsync(HttpClient client, string email, string password = Password)
    {
        using HttpResponseMessage response = await PostLoginAsync(client, email, password);
        response.EnsureSuccessStatusCode();
        return await ReadJsonAsync(response);
    }

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return string.IsNullOrEmpty(json) ? default : JsonDocument.Parse(json).RootElement.Clone();
    }

    public static HttpRequestMessage Authorized(HttpMethod method, string path, string accessToken, object? body = null)
    {
        HttpRequestMessage request = new(method, new Uri(path, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    public static async Task<HttpResponseMessage> SendAuthorizedAsync(HttpClient client, HttpMethod method, string path, string accessToken, object? body = null)
    {
        using HttpRequestMessage request = Authorized(method, path, accessToken, body);
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public static string AccessToken(JsonElement auth) => auth.GetProperty("accessToken").GetString()!;
}
