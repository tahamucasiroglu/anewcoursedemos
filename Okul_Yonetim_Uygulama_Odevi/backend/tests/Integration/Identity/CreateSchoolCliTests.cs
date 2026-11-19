using Microsoft.EntityFrameworkCore;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using OkulYonetim.WebAPI.Cli;
using Shouldly;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Identity;

/// <summary>Okul açma komutu (spec 0004 AC-1, AC-2): gerçek komut kodu, gizli şifre girişi yerine verilen okuyucu ile.</summary>
[Trait("Category", "Integration")]
public sealed class CreateSchoolCliTests
{
    private const string Secret = "Kurucu-Gizli-Sifre-88";
    private readonly TestDatabase database;

    public CreateSchoolCliTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Run_NewFounder_CreatesSchoolBranchFounderAndNeverPrintsPassword()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        string email = UniqueEmail("kurucu");
        string school = "Çınar Koleji " + Guid.NewGuid().ToString("N");
        using StringWriter output = new();

        int exit = await RunAsync(factory, output, school, "Bahçelievler", email, Secret, Secret);

        exit.ShouldBe(0);
        output.ToString().ShouldContain("Okul açıldı");
        output.ToString().ShouldContain("yeni hesap");
        output.ToString().ShouldNotContain(Secret);
        factory.ReadLog().ShouldNotContain(Secret);
        User user = await QueryAsync(factory, c => c.Users.SingleAsync(u => u.Email == email));
        user.PasswordHash.ShouldNotContain(Secret);
        (await QueryAsync(factory, c => c.Tenants.IgnoreQueryFilters().CountAsync(t => t.Name == school))).ShouldBe(1);
        (await QueryAsync(factory, c => c.Branches.IgnoreQueryFilters().CountAsync(b => b.Name == "Bahçelievler"))).ShouldBeGreaterThanOrEqualTo(1);
        (await QueryAsync(factory, c => c.RoleAssignments.IgnoreQueryFilters().CountAsync(r => r.UserId == user.Id && r.Role == Role.Founder && r.BranchId == null))).ShouldBe(1);
        using HttpClient client = factory.CreateClient();
        (await PostLoginAsync(client, email, Secret)).StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("kisa7ch", "kisa7ch", "Şifre en az 8 karakter olmalıdır")]
    [InlineData(Secret, "Baska-Sifre-99", "şifreler aynı değil")]
    public async Task Run_BadPassword_CreatesNothing(string password, string repeat, string expected)
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        string email = UniqueEmail("kurucu");
        using StringWriter output = new();

        int exit = await RunAsync(factory, output, "Okul " + Guid.NewGuid().ToString("N"), "Merkez", email, password, repeat);

        exit.ShouldBe(1);
        output.ToString().ShouldContain(expected);
        (await QueryAsync(factory, c => c.Users.CountAsync(u => u.Email == email))).ShouldBe(0);
    }

    [Fact]
    public async Task Run_InvalidEmail_CreatesNothing()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        string school = "Okul " + Guid.NewGuid().ToString("N");
        using StringWriter output = new();

        int exit = await RunAsync(factory, output, school, "Merkez", "gecersiz-eposta", Secret, Secret);

        exit.ShouldBe(1);
        output.ToString().ShouldContain("Geçerli bir e-posta girin");
        (await QueryAsync(factory, c => c.Tenants.IgnoreQueryFilters().CountAsync(t => t.Name == school))).ShouldBe(0);
    }

    [Fact]
    public async Task Run_ExistingEmailForAnotherSchool_AddsFounderRoleWithoutChangingPasswordOrAskingIt()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        SchoolSeed first = await CreateSchoolAsync(factory);
        string hashBefore = await QueryAsync(factory, c => c.Users.Where(u => u.Id == first.FounderId).Select(u => u.PasswordHash).SingleAsync());
        int asked = 0;
        using StringWriter output = new();

        int exit = await CreateSchoolCli.RunAsync(factory.Services, Args("İkinci Okul " + Guid.NewGuid().ToString("N"), "Merkez", first.FounderEmail), output, () => { asked++; return "Yeni-Sifre-Olmamali"; }, TestContext.Current.CancellationToken);

        exit.ShouldBe(0);
        asked.ShouldBe(0);
        output.ToString().ShouldContain("mevcut hesap");
        (await QueryAsync(factory, c => c.Users.CountAsync(u => u.Email == first.FounderEmail))).ShouldBe(1);
        (await QueryAsync(factory, c => c.Users.Where(u => u.Id == first.FounderId).Select(u => u.PasswordHash).SingleAsync())).ShouldBe(hashBefore);
        (await QueryAsync(factory, c => c.RoleAssignments.IgnoreQueryFilters().CountAsync(r => r.UserId == first.FounderId && r.Role == Role.Founder))).ShouldBe(2);
    }

    [Fact]
    public async Task Run_SameSchoolSameFounderAgain_SaysAlreadyExistsAndChangesNothing()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        SchoolSeed first = await CreateSchoolAsync(factory);
        int auditBefore = await QueryAsync(factory, c => c.AuditEntries.CountAsync(a => a.TenantId == first.TenantId));
        using StringWriter output = new();

        int exit = await CreateSchoolCli.RunAsync(factory.Services, Args(first.SchoolName.ToUpperInvariant(), "Merkez", first.FounderEmail), output, () => null, TestContext.Current.CancellationToken);

        exit.ShouldBe(0);
        output.ToString().ShouldContain("Zaten var");
        string upper = first.SchoolName.ToUpperInvariant();
        (await QueryAsync(factory, c => c.Tenants.IgnoreQueryFilters().CountAsync(t => t.Name == first.SchoolName || t.Name == upper))).ShouldBe(1);
        (await QueryAsync(factory, c => c.AuditEntries.CountAsync(a => a.TenantId == first.TenantId))).ShouldBe(auditBefore);
    }

    [Fact]
    public async Task Run_MissingArguments_PrintsUsageAndReturns2()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using StringWriter output = new();

        int exit = await CreateSchoolCli.RunAsync(factory.Services, ["--okul", "Yalnız Okul"], output, () => null, TestContext.Current.CancellationToken);

        exit.ShouldBe(2);
        output.ToString().ShouldContain("Kullanım");
    }

    private static Task<int> RunAsync(OkulApiFactory factory, StringWriter output, string school, string branch, string email, params string[] secrets)
    {
        Queue<string> answers = new(secrets);
        return CreateSchoolCli.RunAsync(factory.Services, Args(school, branch, email), output, () => answers.Dequeue(), TestContext.Current.CancellationToken);
    }

    private static string[] Args(string school, string branch, string email) =>
        ["--okul", school, "--sube", branch, "--ad", "Ayşe Kurucu", "--eposta", email];
}
