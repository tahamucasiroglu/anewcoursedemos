using System.Net;
using Microsoft.EntityFrameworkCore;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Academics.AcademicsTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Academics;

/// <summary>
/// Spec 0013 AC-11: log satırlarında ve bildirim log'unda (sahte gönderici <c>LoggingPushNotifier</c>) öğrenci adı, ödev
/// başlığı/açıklaması, ders adı ve puan geçmez (yalnızca kimlikler; istek gövdesi loglanmaz); ödev verme, teslim kaydı,
/// değerlendirme açma ve puan kaydı/düzeltmesi audit kaydı oluşturur (kim, sınıf, kayıt, değişen öğrenci sayısı, ne zaman, IP),
/// audit kaydında puan değeri, ad ve metin yoktur (ayrıntı alanı birebir: yalnızca sınıf ve sayı).
/// </summary>
[Trait("Category", "Integration")]
public sealed class AcademicsPrivacyAndAuditTests
{
    private readonly TestDatabase database;

    public AcademicsPrivacyAndAuditTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task LogsHoldOnlyIds_EveryWriteAudited_WithoutScoresOrText()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.77");
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid gizli = await StudentAsync(client, w.Class, "Gizliöğrenci Adsoyad");

        Guid assignment = await AssignedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Homework("Gizliders", "Gizlibaşlık metni", "Gizliaçıklama metni", "2026-11-25"));
        (await SaveSubmissionsAsync(client, w.Class.Teacher, assignment, (gizli, "NotSubmitted"))).Status.ShouldBe(HttpStatusCode.OK);
        Guid assessment = await OpenedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Exam("Gizlisınavdersi", "Written", "Gizlisınav başlığı", Today));
        (await SaveGradesAsync(client, w.Class.Teacher, assessment, (gizli, 93), (w.Ali, 64))).Status.ShouldBe(HttpStatusCode.OK);
        (await SaveGradesAsync(client, w.Class.Teacher, assessment, (gizli, 94))).Status.ShouldBe(HttpStatusCode.OK);

        string log = factory.ReadLog();
        foreach (string secret in new[] { "Gizliöğrenci", "Gizliders", "Gizlibaşlık", "Gizliaçıklama", "Gizlisınav", "Yeni ödev", "\"score\"" })
        {
            log.ShouldNotContain(secret);
        }

        log.ShouldContain(assignment.ToString());
        string[] lines = log.Split('\n');
        string[] notifications = [.. lines.Where(l => l.Contains("LoggingPushNotifier", StringComparison.Ordinal))];
        notifications.Length.ShouldBe(3, "Ali'nin annesi ve babası, Ayşe'nin annesi; bildirim log'u yalnızca tür ve kimlik");
        notifications.ShouldAllBe(l => l.Contains("\"SchoolNotice\"", StringComparison.Ordinal));

        List<AuditEntry> audit = await AuditAsync(factory, w.Class.School.TenantId, AuditAction.AssignmentCreated, AuditAction.SubmissionsRecorded, AuditAction.AssessmentCreated, AuditAction.GradesRecorded);
        audit.Select(a => (a.Action, a.TargetType, a.TargetId, a.Detail)).ShouldBe(
            [
                (AuditAction.AssignmentCreated, "Assignment", assignment, $"classroom={w.Class.ClassroomId:D};students=3"),
                (AuditAction.SubmissionsRecorded, "Assignment", assignment, $"classroom={w.Class.ClassroomId:D};changed=1"),
                (AuditAction.AssessmentCreated, "Assessment", assessment, $"classroom={w.Class.ClassroomId:D};students=3"),
                (AuditAction.GradesRecorded, "Assessment", assessment, $"classroom={w.Class.ClassroomId:D};changed=2"),
                (AuditAction.GradesRecorded, "Assessment", assessment, $"classroom={w.Class.ClassroomId:D};changed=1"),
            ],
            ignoreOrder: true);
        audit.ShouldAllBe(a => a.ActorUserId == w.Class.TeacherId && a.IpAddress == "203.0.113.77" && a.OccurredAt == Morning);
        string auditJson = string.Join('|', await QueryAsync(factory, x => x.Database
            .SqlQuery<string>($"SELECT row_to_json(a)::text AS \"Value\" FROM audit_entries a WHERE a.tenant_id = {w.Class.School.TenantId}")
            .ToListAsync()));
        auditJson.ShouldNotContain("Gizli");
    }
}
