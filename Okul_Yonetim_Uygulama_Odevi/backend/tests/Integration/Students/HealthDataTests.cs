using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OkulYonetim.Domain.Students;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Students;

/// <summary>
/// Spec 0009 AC-10: sağlık notu ve alerji veritabanında düz metin olarak yoktur (şifreli), kurucunun ve bağlı velinin
/// kartında doğru okunur; log, hata yanıtı ve audit kayıtlarında geçmez; anahtar repoda yoktur. Anahtar ayarlı değilse kart
/// çalışır, sağlık "gösterilemiyor" (Available=false), dolu sağlık kaydetme reddedilir (logda "yapılandırılmamış") ve diğer
/// alanlar kaydedilir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class HealthDataTests
{
    private const string Note = "Astım-NOTU-7f3a; Ventolin taşır";
    private const string Allergy = "Fıstık-ALERJİ-91c2";

    private readonly TestDatabase database;

    public HealthDataTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Health_IsEncryptedAtRest_ReadCorrectlyByFounderAndLinkedGuardian_NeverInLogsOrAudit()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);
        Guid id = await CreateStudentAsync(client, founder, NewStudent(a.BranchId, "Sağlık Öğrencisi", healthNote: Note, allergy: Allergy));
        (string guardian, _, _, _) = await NewGuardianAsync(client, founder, a.TenantId, id);

        Student row = (await StudentRowsAsync(factory, a.TenantId)).ShouldHaveSingleItem();
        row.HealthNoteCipher.ShouldNotBeNullOrEmpty();
        row.AllergyCipher.ShouldNotBeNullOrEmpty();
        row.HealthNoteCipher.ShouldNotContain("Astım");
        row.AllergyCipher.ShouldNotContain("Fıstık");
        string dump = await RawStudentRowAsync(factory, id);
        dump.ShouldNotContain("NOTU-7f3a");
        dump.ShouldNotContain("ALERJİ-91c2");

        JsonElement founderHealth = (await CardAsync(client, founder, id)).GetProperty("health");
        founderHealth.GetProperty("available").GetBoolean().ShouldBeTrue();
        founderHealth.GetProperty("healthNote").GetString().ShouldBe(Note);
        founderHealth.GetProperty("allergy").GetString().ShouldBe(Allergy);
        JsonElement guardianHealth = (await CallAsync(client, HttpMethod.Get, $"/api/my/children/{id}", guardian)).Body.GetProperty("health");
        guardianHealth.GetProperty("healthNote").GetString().ShouldBe(Note);
        guardianHealth.GetProperty("allergy").GetString().ShouldBe(Allergy);

        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/students/{id}/health", founder, new { healthNote = "Yeni-NOT-55", allergy = "" })).ShouldBe(HttpStatusCode.OK);
        JsonElement updated = (await CardAsync(client, founder, id)).GetProperty("health");
        updated.GetProperty("healthNote").GetString().ShouldBe("Yeni-NOT-55");
        updated.GetProperty("allergy").ValueKind.ShouldBe(JsonValueKind.Null);
        var (tooLong, problem, problemRaw) = await CallAsync(client, HttpMethod.Put, $"/api/admin/students/{id}/health", founder, new { healthNote = Note + new string('x', 1000) });
        tooLong.ShouldBe(HttpStatusCode.BadRequest);
        Messages(problem).ShouldContain("Sağlık notu en fazla 1000 karakter olabilir");
        Regex.Unescape(problemRaw).ShouldNotContain("NOTU-7f3a", customMessage: "hata yanıtı girilen sağlık metnini geri vermez");

        string log = factory.ReadLog();
        log.ShouldNotContain("NOTU-7f3a");
        log.ShouldNotContain("ALERJİ-91c2");
        log.ShouldNotContain("Yeni-NOT-55");
        string audit = await AuditDumpAsync(factory, a.TenantId);
        audit.ShouldNotContain("NOTU-7f3a");
        audit.ShouldNotContain("ALERJİ-91c2");
        audit.ShouldNotContain("Yeni-NOT-55");
    }

    [Fact]
    public async Task WithoutKey_CardWorks_HealthNotShown_FilledHealthRejected_OtherFieldsSaved()
    {
        Guid id;
        SchoolSeed a;
        using (OkulApiFactory keyed = Factory(this.database))
        {
            using HttpClient keyedClient = keyed.CreateClient();
            (a, string founderKeyed) = await SchoolAsync(keyed, keyedClient);
            id = await CreateStudentAsync(keyedClient, founderKeyed, NewStudent(a.BranchId, "Anahtarsız", healthNote: Note, allergy: Allergy));
        }

        using OkulApiFactory factory = Factory(this.database, personalDataKey: null);
        using HttpClient client = factory.CreateClient();
        string founder = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        (string guardian, _, _, _) = await NewGuardianAsync(client, founder, a.TenantId, id);

        JsonElement card = await CardAsync(client, founder, id);
        card.GetProperty("fullName").GetString().ShouldBe("Anahtarsız");
        card.GetProperty("health").GetProperty("available").GetBoolean().ShouldBeFalse();
        card.GetProperty("health").GetProperty("healthNote").ValueKind.ShouldBe(JsonValueKind.Null);
        JsonElement child = (await CallAsync(client, HttpMethod.Get, $"/api/my/children/{id}", guardian)).Body;
        child.GetProperty("health").GetProperty("available").GetBoolean().ShouldBeFalse();

        var (rejected, problem, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/students/{id}/health", founder, new { healthNote = "yeni", allergy = (string?)null });
        rejected.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Sağlık bilgisi şu anda kaydedilemiyor.");
        var (rejectedCreate, problem2, _) = await CallAsync(client, HttpMethod.Post, "/api/admin/students", founder, NewStudent(a.BranchId, "Yeni Sağlıklı", allergy: "polen"));
        rejectedCreate.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem2).ShouldBe("Sağlık bilgisi şu anda kaydedilemiyor.");
        factory.ReadLog().ShouldContain("yapılandırılmamış");

        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/students/{id}", founder, NewStudent(a.BranchId, "Anahtarsız Düzenlendi"))).ShouldBe(HttpStatusCode.OK);
        await CreateStudentAsync(client, founder, NewStudent(a.BranchId, "Sağlıksız Kayıt"));
        Student row = (await StudentRowsAsync(factory, a.TenantId)).Single(s => s.Id == id);
        row.FullName.ShouldBe("Anahtarsız Düzenlendi");
        row.HealthNoteCipher.ShouldNotBeNull("kayıtlı şifreli sağlık bilgisi silinmedi");
        (await StudentRowsAsync(factory, a.TenantId)).Count.ShouldBe(2);
    }

    [Fact]
    public async Task WithoutKey_ClearingStoredHealth_Rejected_StoredCipherUnchanged_NotAudited()
    {
        Guid id;
        SchoolSeed a;
        using (OkulApiFactory keyed = Factory(this.database))
        {
            using HttpClient keyedClient = keyed.CreateClient();
            (a, string founderKeyed) = await SchoolAsync(keyed, keyedClient);
            id = await CreateStudentAsync(keyedClient, founderKeyed, NewStudent(a.BranchId, "Silinmesin", healthNote: Note, allergy: Allergy));
        }

        using OkulApiFactory factory = Factory(this.database, personalDataKey: null);
        using HttpClient client = factory.CreateClient();
        string founder = await TokenForSchoolAsync(client, a.FounderEmail, a.TenantId);
        Student before = (await StudentRowsAsync(factory, a.TenantId)).Single(s => s.Id == id);
        string auditBefore = await AuditDumpAsync(factory, a.TenantId);

        var (status, problem, _) = await CallAsync(client, HttpMethod.Put, $"/api/admin/students/{id}/health", founder, new { healthNote = "", allergy = (string?)null });

        status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(problem).ShouldBe("Sağlık bilgisi şu anda kaydedilemiyor.");
        Student after = (await StudentRowsAsync(factory, a.TenantId)).Single(s => s.Id == id);
        after.HealthNoteCipher.ShouldBe(before.HealthNoteCipher, "kayıtlı şifreli sağlık notu aynen kalır");
        after.AllergyCipher.ShouldBe(before.AllergyCipher, "kayıtlı şifreli alerji aynen kalır");
        (await AuditDumpAsync(factory, a.TenantId)).ShouldBe(auditBefore, "reddedilen istek audit'e 'güncellendi' yazmaz");
    }

    [Fact]
    public void EncryptionKey_IsNotInRepositorySettings()
    {
        string backend = FindBackendRoot();
        foreach (string file in Directory.EnumerateFiles(Path.Combine(backend, "src", "WebAPI"), "appsettings*.json"))
        {
            using JsonDocument settings = JsonDocument.Parse(File.ReadAllText(file));
            if (settings.RootElement.TryGetProperty("PersonalData", out JsonElement section) && section.TryGetProperty("EncryptionKey", out JsonElement key))
            {
                key.GetString().ShouldBeNullOrEmpty(file);
            }
        }
    }

    private static async Task<string> RawStudentRowAsync(OkulApiFactory factory, Guid id) =>
        string.Join('|', await QueryAsync(factory, c => c.Database
            .SqlQuery<string>($"SELECT row_to_json(s)::text AS \"Value\" FROM students s WHERE s.id = {id}")
            .ToListAsync()));

    private static async Task<string> AuditDumpAsync(OkulApiFactory factory, Guid tenantId) =>
        string.Join('|', await QueryAsync(factory, c => c.Database
            .SqlQuery<string>($"SELECT row_to_json(a)::text AS \"Value\" FROM audit_entries a WHERE a.tenant_id = {tenantId}")
            .ToListAsync()));

    private static string FindBackendRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("backend/Directory.Build.props bulunamadı");
    }
}
