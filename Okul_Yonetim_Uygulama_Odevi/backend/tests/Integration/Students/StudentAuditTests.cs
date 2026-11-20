using System.Net;
using Microsoft.EntityFrameworkCore;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Tests.Integration.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Students;

/// <summary>
/// Spec 0009 AC-12: öğrenci ekleme, düzenleme, ayrıldı/geri al, kart görüntüleme (kurucu ve veli ayrı ayrı), sağlık okuma,
/// veli bağlama/kaldırma ve teslim kişisi değişikliği audit kaydı oluşturur (kim, ne, hangi öğrenci, ne zaman, IP); veli için
/// hesap açılışı ve rol verilmesi audit'e geçer; geçici şifre, öğrenci adı ve sağlık bilgisi log ve audit kayıtlarında geçmez
/// (yalnızca kimlikler). Reddedilen işlem yazılmaz.
/// </summary>
[Trait("Category", "Integration")]
public sealed class StudentAuditTests
{
    private const string StudentName = "Gizliadlı Öğrenci-Q7";
    private const string Note = "SAĞLIK-Z9-notu";
    private const string Allergy = "ALERJİ-Z9";

    private readonly TestDatabase database;

    public StudentAuditTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task EveryStudentAction_IsAudited_WithActorIpAndStudent_NoPersonalDataInLogOrAudit()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.90");
        (SchoolSeed a, string founder) = await SchoolAsync(factory, client);

        Guid id = await CreateStudentAsync(client, founder, NewStudent(a.BranchId, StudentName, healthNote: Note, allergy: Allergy));
        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/students/{id}", founder, NewStudent(a.BranchId, StudentName + " 2"))).ShouldBe(HttpStatusCode.OK);
        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/students/{id}/health", founder, new { healthNote = Note, allergy = Allergy })).ShouldBe(HttpStatusCode.OK);
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/students/{id}/deactivate", founder)).ShouldBe(HttpStatusCode.OK);
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/students/{id}/activate", founder)).ShouldBe(HttpStatusCode.OK);
        await CardAsync(client, founder, id);
        string guardianEmail = UniqueEmail("denetimveli");
        var (_, linked, _) = await CallAsync(client, HttpMethod.Post, $"/api/admin/students/{id}/guardians", founder, Guardian(guardianEmail, "Veli Adı-K3"));
        Guid guardianId = linked.GetProperty("userId").GetGuid();
        string password = linked.GetProperty("temporaryPassword").GetString()!;
        (await StatusAsync(client, HttpMethod.Put, $"/api/admin/students/{id}/pickup-persons", founder, new { persons = new[] { new { fullName = "Teslim Kişi-P5", relationship = "Dede", phone = "05321234567" } } })).ShouldBe(HttpStatusCode.OK);
        string guardian = await TokenForSchoolAsync(client, guardianEmail, a.TenantId, password);
        (await StatusAsync(client, HttpMethod.Get, $"/api/my/children/{id}", guardian)).ShouldBe(HttpStatusCode.OK);
        (await StatusAsync(client, HttpMethod.Delete, $"/api/admin/students/{id}/guardians/{guardianId}", founder)).ShouldBe(HttpStatusCode.OK);
        (await StatusAsync(client, HttpMethod.Post, "/api/admin/students", founder, NewStudent(a.BranchId, string.Empty))).ShouldBe(HttpStatusCode.BadRequest);

        List<AuditEntry> entries = await QueryAsync(factory, c => c.AuditEntries.Where(e => e.TenantId == a.TenantId && e.TargetId == id).OrderBy(e => e.Id).ToListAsync());
        List<AuditEntry> founderEntries = [.. entries.Where(e => e.ActorUserId == a.FounderId)];
        founderEntries.Select(e => e.Action).ShouldBe(
            [AuditAction.StudentCreated, AuditAction.StudentUpdated, AuditAction.StudentHealthUpdated, AuditAction.StudentLeft, AuditAction.StudentReinstated,
             AuditAction.StudentViewed, AuditAction.StudentHealthRead, AuditAction.GuardianLinked, AuditAction.PickupPersonsChanged, AuditAction.GuardianUnlinked],
            ignoreOrder: true,
            customMessage: "her işlem bir kez; reddedilen ekleme yazılmaz");
        entries.Where(e => e.ActorUserId == guardianId).Select(e => e.Action).ShouldBe([AuditAction.StudentViewed, AuditAction.StudentHealthRead], ignoreOrder: true);
        entries.ShouldAllBe(e => e.IpAddress == "203.0.113.90" && e.OccurredAt == Now && e.TargetType == "Student");
        List<AuditAction> accountActions = await QueryAsync(factory, c => c.AuditEntries
            .Where(e => e.TenantId == a.TenantId && e.ActorUserId == a.FounderId && (e.Action == AuditAction.UserCreated || e.Action == AuditAction.MemberAdded || e.Action == AuditAction.RoleAssigned))
            .Select(e => e.Action).ToListAsync());
        accountActions.ShouldBe([AuditAction.UserCreated, AuditAction.MemberAdded, AuditAction.RoleAssigned], ignoreOrder: true);

        string log = factory.ReadLog();
        string audit = string.Join('|', await QueryAsync(factory, c => c.Database
            .SqlQuery<string>($"SELECT row_to_json(a)::text AS \"Value\" FROM audit_entries a WHERE a.tenant_id = {a.TenantId}")
            .ToListAsync()));
        foreach (string secret in new[] { password, "Gizliadlı", "Öğrenci-Q7", Note, Allergy, "Veli Adı-K3", "Teslim Kişi-P5", guardianEmail, "5321234567" })
        {
            log.ShouldNotContain(secret);
            audit.ShouldNotContain(secret);
        }

        log.ShouldContain(id.ToString());
    }
}
