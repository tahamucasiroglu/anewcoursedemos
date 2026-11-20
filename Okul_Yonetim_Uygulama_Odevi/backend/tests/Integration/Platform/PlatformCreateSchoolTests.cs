using System.Net;
using System.Text.RegularExpressions;
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

/// <summary>Spec 0007 AC-7, AC-8: panelden okul ekle — yeni kurucuya geçici şifre bir kez; var olan kişi; hatalar.</summary>
[Trait("Category", "Integration")]
public sealed class PlatformCreateSchoolTests
{
    private readonly TestDatabase database;

    public PlatformCreateSchoolTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task NewFounder_GetsTemporaryPasswordOnce_AndCanSignInAsFounder()
    {
        FakeTimeProvider time = Clock();
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed admin, string token, _) = await SignedInPlatformAdminAsync(factory, client, time);
        string email = UniqueEmail("yeni-kurucu");
        string name = $"Panel Okulu {Guid.NewGuid():N}";

        var created = await CallAsync(client, HttpMethod.Post, Schools, token, new { schoolName = name, branchName = "Merkez", founderName = "Yeni Kurucu", founderEmail = email });

        created.Status.ShouldBe(HttpStatusCode.OK, created.Raw);
        created.Body.GetProperty("existingAccount").GetBoolean().ShouldBeFalse();
        string temporary = created.Body.GetProperty("temporaryPassword").GetString()!;
        temporary.ShouldMatch(TemporaryPasswordPattern);
        Guid tenantId = created.Body.GetProperty("tenantId").GetGuid();
        (await CallAsync(client, HttpMethod.Get, $"{Schools}?search={Uri.EscapeDataString(name)}", token)).Raw.ShouldNotContain(temporary);
        JsonElement me = (await CallAsync(client, HttpMethod.Get, "/api/me", await TokenForSchoolAsync(client, email, tenantId, temporary))).Body;
        me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ShouldBe(["Founder"]);
        me.GetProperty("tenant").GetProperty("name").GetString().ShouldBe(name);
        List<AuditEntry> audit = await QueryAsync(factory, c => c.AuditEntries.Where(e => e.TenantId == tenantId && e.Action != AuditAction.LoginSucceeded).ToListAsync());
        audit.Select(e => e.Action).ShouldBe([AuditAction.TenantCreated, AuditAction.BranchCreated, AuditAction.UserCreated, AuditAction.RoleAssigned], ignoreOrder: true);
        audit.ShouldAllBe(e => e.ActorUserId == admin.FounderId && e.Origin == "platform paneli");
    }

    [Fact]
    public async Task ExistingPerson_GetsFounderRole_PasswordUnchanged_AndDuplicateIsRejected()
    {
        FakeTimeProvider time = Clock();
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        (_, string token, _) = await SignedInPlatformAdminAsync(factory, client, time);
        SchoolSeed other = await CreateSchoolAsync(factory);
        string name = $"İkinci Okul {Guid.NewGuid():N}";
        object body = new { schoolName = name, branchName = "Merkez", founderName = "Başka Ad", founderEmail = other.FounderEmail };

        var created = await CallAsync(client, HttpMethod.Post, Schools, token, body);
        var duplicate = await CallAsync(client, HttpMethod.Post, Schools, token, body);

        created.Status.ShouldBe(HttpStatusCode.OK, created.Raw);
        created.Body.GetProperty("existingAccount").GetBoolean().ShouldBeTrue();
        created.Body.GetProperty("temporaryPassword").ValueKind.ShouldBe(JsonValueKind.Null);
        Guid tenantId = created.Body.GetProperty("tenantId").GetGuid();
        (await CallAsync(client, HttpMethod.Get, "/api/me", await TokenForSchoolAsync(client, other.FounderEmail, tenantId))).Body
            .GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ShouldBe(["Founder"]);
        duplicate.Status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(duplicate.Body).ShouldBe("Bu okul zaten var");
        (await QueryAsync(factory, c => c.Tenants.IgnoreQueryFilters().CountAsync(t => t.Name == name))).ShouldBe(1);
    }

    [Theory]
    [InlineData("", "Merkez", "Ad Soyad", "x@ornek.test", "Okul adı zorunludur")]
    [InlineData("Okul", " ", "Ad Soyad", "x@ornek.test", "Şube adı zorunludur")]
    [InlineData("Okul", "Merkez", "", "x@ornek.test", "Ad soyad zorunludur")]
    [InlineData("Okul", "Merkez", "Ad Soyad", "gecersiz", "Geçerli bir e-posta girin")]
    [InlineData("Okul", "Merkez", "Ad Soyad", "", "E-posta zorunludur")]
    [InlineData("UZUN", "Merkez", "Ad Soyad", "x@ornek.test", "En fazla 100 karakter olabilir")]
    public async Task InvalidFields_AreRejected_AndNothingIsCreated(string school, string branch, string founder, string email, string message)
    {
        FakeTimeProvider time = Clock();
        using OkulApiFactory factory = new(this.database.ConnectionString, timeProvider: time);
        using HttpClient client = factory.CreateClient();
        (_, string token, _) = await SignedInPlatformAdminAsync(factory, client, time);
        string tag = Guid.NewGuid().ToString("N");
        string schoolName = school switch { "UZUN" => new string('a', 101), "Okul" => $"Okul {tag}", _ => school };
        string founderEmail = email == "x@ornek.test" ? UniqueEmail("hatali") : email;

        var result = await CallAsync(client, HttpMethod.Post, Schools, token, new { schoolName, branchName = branch, founderName = founder, founderEmail });

        result.Status.ShouldBe(HttpStatusCode.BadRequest);
        Regex.Unescape(result.Raw).ShouldContain(message);
        (await QueryAsync(factory, c => c.Tenants.IgnoreQueryFilters().CountAsync(t => t.Name == schoolName))).ShouldBe(0);
        (await QueryAsync(factory, c => c.Users.CountAsync(u => u.Email == founderEmail))).ShouldBe(0);
    }
}
