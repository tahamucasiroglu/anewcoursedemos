using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;
using static OkulYonetim.Tests.Integration.Platform.PlatformTestData;

namespace OkulYonetim.Tests.Integration.Platform;

/// <summary>Spec 0007 AC-6: okul listesi — sıralama, özet alanlar, Türkçe arama, durum süzgeci, 25'lik sayfalar.</summary>
[Trait("Category", "Integration")]
public sealed class PlatformSchoolListTests
{
    private readonly TestDatabase database;

    public PlatformSchoolListTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Rows_HaveSummaryOnly_AndTurkishSearchAndStatusFilterWork()
    {
        FakeTimeProvider time = Clock();
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        (_, string token, _) = await SignedInPlatformAdminAsync(factory, client, time);
        string tag = Guid.NewGuid().ToString("N")[..8];
        SchoolSeed izmir = await CreateSchoolAsync(factory, schoolName: $"İzmir Işık Koleji {tag}");
        await AddMemberAsync(factory, UniqueEmail("ogretmen"), izmir.TenantId, Role.Teacher, izmir.BranchId);
        await AddBranchAsync(factory, izmir.TenantId, "Bornova");
        SchoolSeed ankara = await CreateSchoolAsync(factory, schoolName: $"Ankara Çınar Koleji {tag}");
        (await StatusAsync(client, HttpMethod.Post, $"{Schools}/{ankara.TenantId}/deactivate", token)).ShouldBe(HttpStatusCode.OK);

        var all = await CallAsync(client, HttpMethod.Get, $"{Schools}?search={tag}", token);
        all.Status.ShouldBe(HttpStatusCode.OK);
        all.Body.GetProperty("total").GetInt32().ShouldBe(2);
        JsonElement[] rows = [.. all.Body.GetProperty("items").EnumerateArray()];
        rows.Select(r => r.GetProperty("name").GetString()).ShouldBe([$"Ankara Çınar Koleji {tag}", $"İzmir Işık Koleji {tag}"]);
        JsonElement row = rows[1];
        row.EnumerateObject().Select(p => p.Name).ShouldBe(["tenantId", "name", "createdAt", "isActive", "activeBranchCount", "activeMemberCount", "founders"]);
        row.GetProperty("activeBranchCount").GetInt32().ShouldBe(2);
        row.GetProperty("activeMemberCount").GetInt32().ShouldBe(2);
        JsonElement founder = row.GetProperty("founders").EnumerateArray().Single();
        founder.EnumerateObject().Select(p => p.Name).ShouldBe(["fullName", "email"]);
        founder.GetProperty("email").GetString().ShouldBe(izmir.FounderEmail);
        all.Raw.ShouldNotContain("phone", Case.Insensitive);
        all.Raw.ShouldNotContain("ogretmen-");

        var turkish = await CallAsync(client, HttpMethod.Get, $"{Schools}?search={Uri.EscapeDataString("ışık koleji " + tag)}", token);
        turkish.Body.GetProperty("items").EnumerateArray().Single().GetProperty("tenantId").GetGuid().ShouldBe(izmir.TenantId);
        var inactive = await CallAsync(client, HttpMethod.Get, $"{Schools}?search={tag}&status=inactive", token);
        inactive.Body.GetProperty("items").EnumerateArray().Single().GetProperty("isActive").GetBoolean().ShouldBeFalse();
        var active = await CallAsync(client, HttpMethod.Get, $"{Schools}?search={tag}&status=active", token);
        active.Body.GetProperty("items").EnumerateArray().Single().GetProperty("tenantId").GetGuid().ShouldBe(izmir.TenantId);
        var none = await CallAsync(client, HttpMethod.Get, $"{Schools}?search=yok-{tag}", token);
        none.Body.GetProperty("total").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task MoreThan25Schools_ArePaged()
    {
        FakeTimeProvider time = Clock();
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        (_, string token, _) = await SignedInPlatformAdminAsync(factory, client, time);
        string tag = Guid.NewGuid().ToString("N")[..8];
        string founder = UniqueEmail("cok-okul");
        for (int i = 1; i <= 26; i++)
        {
            await CreateSchoolAsync(factory, founder, schoolName: $"Sayfa {tag} {i:D2}");
        }

        var first = await CallAsync(client, HttpMethod.Get, $"{Schools}?search={tag}", token);
        var second = await CallAsync(client, HttpMethod.Get, $"{Schools}?search={tag}&page=2", token);

        first.Body.GetProperty("total").GetInt32().ShouldBe(26);
        first.Body.GetProperty("pageSize").GetInt32().ShouldBe(25);
        first.Body.GetProperty("items").GetArrayLength().ShouldBe(25);
        second.Body.GetProperty("items").EnumerateArray().Single().GetProperty("name").GetString().ShouldBe($"Sayfa {tag} 26");
    }
}
