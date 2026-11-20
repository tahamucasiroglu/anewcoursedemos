using System.Net;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Students;

/// <summary>
/// Spec 0009 AC-1: öğretmen, veli ve muhasebe rolündeki kişi öğrenci yönetimi uçlarını doğrudan denerse 403 alır ve hiçbir
/// şey değişmez; veli rolü olmayan kişi (kurucu, öğretmen) "Çocuklarım" uçlarında 403 alır; girişsiz istek 401.
/// </summary>
[Trait("Category", "Integration")]
public sealed class StudentAuthorizationTests
{
    private readonly TestDatabase database;

    public StudentAuthorizationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Theory]
    [InlineData(Role.Teacher)]
    [InlineData(Role.Accounting)]
    [InlineData(Role.Guardian)]
    public async Task AdminEndpoints_NonFounderRole_Returns403_AndChangesNothing(Role role)
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);
        Guid student = await CreateStudentAsync(client, founder, NewStudent(a.BranchId, "Ayşe Kaya", schoolNumber: "100"));
        string email = UniqueEmail("rol");
        await AddMemberAsync(factory, email, a.TenantId, role, a.BranchId);
        string token = await TokenForSchoolAsync(client, email, a.TenantId);

        (HttpMethod Method, string Path, object? Body)[] calls =
        [
            (HttpMethod.Get, "/api/admin/students", null),
            (HttpMethod.Get, $"/api/admin/students/{student}", null),
            (HttpMethod.Post, "/api/admin/students", NewStudent(a.BranchId, "Yetkisiz Kayıt")),
            (HttpMethod.Put, $"/api/admin/students/{student}", NewStudent(a.BranchId, "Değişti")),
            (HttpMethod.Put, $"/api/admin/students/{student}/health", new { healthNote = "x", allergy = "y" }),
            (HttpMethod.Post, $"/api/admin/students/{student}/deactivate", null),
            (HttpMethod.Post, $"/api/admin/students/{student}/activate", null),
            (HttpMethod.Post, $"/api/admin/students/{student}/guardians", Guardian(UniqueEmail("v"))),
            (HttpMethod.Delete, $"/api/admin/students/{student}/guardians/{a.FounderId}", null),
            (HttpMethod.Put, $"/api/admin/students/{student}/pickup-persons", new { persons = new[] { new { fullName = "Dede", relationship = "Dede", phone = "05321234567" } } }),
        ];
        foreach ((HttpMethod method, string path, object? body) in calls)
        {
            (await StatusAsync(client, method, path, token, body)).ShouldBe(HttpStatusCode.Forbidden, $"{method} {path}");
        }

        (await StudentRowsAsync(factory, a.TenantId)).ShouldHaveSingleItem().FullName.ShouldBe("Ayşe Kaya");
        var card = await CardAsync(client, founder, student);
        card.GetProperty("isActive").GetBoolean().ShouldBeTrue();
        card.GetProperty("guardians").GetArrayLength().ShouldBe(0);
        card.GetProperty("pickupPersons").GetArrayLength().ShouldBe(0);
        card.GetProperty("health").GetProperty("healthNote").ValueKind.ShouldBe(System.Text.Json.JsonValueKind.Null);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(Role.Teacher)]
    public async Task MyChildren_PersonWithoutGuardianRole_Returns403(Role? role)
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);
        Guid student = await CreateStudentAsync(client, founder, NewStudent(a.BranchId));
        string token = founder;
        if (role is { } r)
        {
            string email = UniqueEmail("ogretmen");
            await AddMemberAsync(factory, email, a.TenantId, r, a.BranchId);
            token = await TokenForSchoolAsync(client, email, a.TenantId);
        }

        (await StatusAsync(client, HttpMethod.Get, "/api/my/children", token)).ShouldBe(HttpStatusCode.Forbidden);
        (await StatusAsync(client, HttpMethod.Get, $"/api/my/children/{student}", token)).ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task StudentEndpoints_Anonymous_Return401()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        Guid any = Guid.CreateVersion7();

        foreach (string path in new[] { "/api/admin/students", $"/api/admin/students/{any}", "/api/my/children", $"/api/my/children/{any}" })
        {
            using HttpResponseMessage response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, path);
        }

        using HttpResponseMessage post = await PostJsonAsync(client, "/api/admin/students", NewStudent(any));
        post.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
