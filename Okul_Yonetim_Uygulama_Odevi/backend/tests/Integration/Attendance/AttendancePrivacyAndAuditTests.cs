using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Tests.Integration.Account;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Attendance;

/// <summary>
/// Spec 0010 AC-12: bildirim ve e-posta gövdesinde öğrenci adı, gün, okul adı ve durumdan başka kişisel veri yok (sağlık
/// notu, sınıf arkadaşı, veli adı/e-postası yok); loglarda e-posta adresi, gövde ve öğrenci adı geçmez (yalnızca kimlikler);
/// yoklama kaydı ve düzeltmesi audit'te (kim, sınıf, gün, değişen öğrenci sayısı, IP) — audit kaydında da ad yok.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AttendancePrivacyAndAuditTests
{
    private readonly TestDatabase database;

    public AttendancePrivacyAndAuditTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task NoticeAndLogs_HoldNoExtraPersonalData_RecordAndCorrectionAudited()
    {
        CapturingOutbox outbox = new();
        using OkulApiFactory factory = Factory(this.database, outbox);
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.55");
        AttendanceClass c = await ClassAsync(factory, client);
        Guid ali = await CreateStudentAsync(client, c.Founder, NewStudent(c.School.BranchId, "Gizlialan Öğrencisi", classroomId: c.ClassroomId, healthNote: "Astım-notu-gizli", allergy: "Fıstık-gizli"));
        Guid mate = await StudentAsync(client, c, "Sınıfarkadaşı Kişi");
        (string mother, Guid motherId, string motherEmail, _) = await NewGuardianAsync(client, c.Founder, c.School.TenantId, ali);

        await SavedAsync(client, c.Teacher, c.ClassroomId, [(ali, "Absent"), (mate, "Present")]);
        await SavedAsync(client, c.Teacher, c.ClassroomId, [(ali, "Present"), (mate, "Late")]);

        EmailMessage[] emails = outbox.To(motherEmail);
        emails.Length.ShouldBe(2);
        foreach (EmailMessage email in emails)
        {
            string all = email.Subject + email.Body;
            all.ShouldContain("Gizlialan Öğrencisi");
            all.ShouldNotContain("Astım");
            all.ShouldNotContain("Fıstık");
            all.ShouldNotContain("Sınıfarkadaşı");
            all.ShouldNotContain("5-A", customMessage: "sınıf adı yok");
            all.ShouldNotContain(motherEmail);
            all.ShouldNotContain("Veli Kişi", customMessage: "velinin adı gövdede yok");
        }

        string notices = (await NoticesAsync(client, mother)).Raw;
        notices.ShouldNotContain("Astım");
        notices.ShouldNotContain("Sınıfarkadaşı");

        string log = factory.ReadLog();
        log.ShouldNotContain(motherEmail);
        log.ShouldNotContain("Gizlialan");
        log.ShouldNotContain("Sınıfarkadaşı");
        log.ShouldNotContain("yoklamada gelmedi");
        log.ShouldNotContain("düzeltildi: okulda");
        log.ShouldContain(c.ClassroomId.ToString());

        Guid sheetId = await QueryAsync(factory, x => x.AttendanceSheets.IgnoreQueryFilters().Where(s => s.ClassroomId == c.ClassroomId).Select(s => s.Id).SingleAsync());
        List<AuditEntry> audit = await AuditAsync(factory, c.School.TenantId, AuditAction.AttendanceRecorded, AuditAction.AttendanceCorrected);
        audit.Select(a => (a.Action, a.Detail)).ShouldBe(
            [
                (AuditAction.AttendanceRecorded, $"classroom={c.ClassroomId:D};day={Today};changed=2"),
                (AuditAction.AttendanceCorrected, $"classroom={c.ClassroomId:D};day={Today};changed=2"),
            ],
            ignoreOrder: true);
        audit.ShouldAllBe(a => a.ActorUserId == c.TeacherId && a.TargetType == "AttendanceSheet" && a.TargetId == sheetId && a.IpAddress == "203.0.113.55" && a.OccurredAt == Morning);
        string auditJson = string.Join('|', await QueryAsync(factory, x => x.Database
            .SqlQuery<string>($"SELECT row_to_json(a)::text AS \"Value\" FROM audit_entries a WHERE a.tenant_id = {c.School.TenantId}")
            .ToListAsync()));
        auditJson.ShouldNotContain("Gizlialan");
        auditJson.ShouldNotContain(motherEmail);
        motherId.ShouldNotBe(Guid.Empty);
    }
}
