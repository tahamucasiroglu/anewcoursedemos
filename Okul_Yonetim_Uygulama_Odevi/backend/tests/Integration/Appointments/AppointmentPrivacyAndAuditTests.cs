using System.Net;
using Microsoft.EntityFrameworkCore;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Appointments.AppointmentsTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Appointments;

/// <summary>
/// Spec 0016 AC-12: log ve bildirim log satırlarında konu, ret/iptal nedeni ve kişi adı (öğretmen, veli, öğrenci) geçmez — yalnızca
/// kimlikler; müsaitlik açma/kaldırma Information log'unda kimliklerle; talep, onay, ret ve iptal audit kaydı oluşturur (kim, ne, hangi
/// randevu, ne zaman, IP) ve audit ayrıntısı yalnızca kimlik/durum taşır (konu, neden, ad yok).
/// </summary>
[Trait("Category", "Integration")]
public sealed class AppointmentPrivacyAndAuditTests
{
    private readonly TestDatabase database;

    public AppointmentPrivacyAndAuditTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task LogsHoldOnlyIds_EveryChangeAudited_WithoutText()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.77");
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid gizli = await StudentAsync(client, w.Class, "Gizliöğrenci Adsoyad");
        ParentAccount parent = await ParentAsync(client, w.Class, gizli);
        string teacherName = await QueryAsync(factory, c => c.Users.Where(u => u.Id == w.Class.TeacherId).Select(u => u.FullName).SingleAsync());
        string parentName = await QueryAsync(factory, c => c.Users.Where(u => u.Id == parent.UserId).Select(u => u.FullName).SingleAsync());
        await OpenedAsync(client, w.Class.Teacher, start: "14:00", end: "15:00", durationMinutes: 30);
        Guid[] free = await FreeSlotIdsAsync(client, parent.Token, w.Class.TeacherId, gizli);

        Guid rejected = await RequestedAsync(client, parent.Token, free[0], gizli, "Gizlikonu birinci");
        (await RespondAsync(client, w.Class.Teacher, rejected, "Reject", "Gizlineden ret")).Status.ShouldBe(HttpStatusCode.OK);
        Guid cancelled = await RequestedAsync(client, parent.Token, free[1], gizli, "Gizlikonu ikinci");
        (await RespondAsync(client, w.Class.Teacher, cancelled, "Approve")).Status.ShouldBe(HttpStatusCode.OK);
        (await CancelAsync(client, w.Class.Teacher, cancelled, "Gizlineden iptal")).Status.ShouldBe(HttpStatusCode.OK);
        Guid byGuardian = await RequestedAsync(client, parent.Token, free[0], gizli, "Gizlikonu üçüncü");
        (await CancelAsync(client, parent.Token, byGuardian)).Status.ShouldBe(HttpStatusCode.OK);
        await OpenedAsync(client, w.Class.Teacher, start: "16:00", end: "16:30", durationMinutes: 30);
        Guid extra = (await SlotRowsAsync(factory, w.Class.School.TenantId)).Single(s => s.StartsAt == At1400.AddHours(2)).Id;
        (await RemoveSlotAsync(client, w.Class.Teacher, extra)).Status.ShouldBe(HttpStatusCode.NoContent);

        string log = factory.ReadLog();
        foreach (string secret in new[] { "Gizlikonu", "Gizlineden", "Gizliöğrenci", teacherName, parentName, "Yeni randevu talebi", "Randevunuz" })
        {
            log.ShouldNotContain(secret);
        }

        log.ShouldContain(rejected.ToString());
        string[] lines = log.Split('\n');
        string opened = lines.First(l => l.Contains("Müsaitlik açıldı", StringComparison.Ordinal));
        opened.ShouldContain(w.Class.TeacherId.ToString());
        opened.ShouldContain(w.Class.School.TenantId.ToString());
        lines.ShouldContain(l => l.Contains("Görüşme saati kaldırıldı", StringComparison.Ordinal) && l.Contains(extra.ToString(), StringComparison.Ordinal));
        string[] notifications = [.. lines.Where(l => l.Contains("LoggingPushNotifier", StringComparison.Ordinal))];
        notifications.Length.ShouldBeGreaterThan(0);
        notifications.ShouldAllBe(l => !l.Contains("Gizli", StringComparison.Ordinal));

        List<AuditEntry> audit = await AuditAsync(factory, w.Class.School.TenantId, AuditAction.AppointmentRequested, AuditAction.AppointmentApproved, AuditAction.AppointmentRejected, AuditAction.AppointmentCancelled);
        audit.Select(a => (a.Action, a.TargetId, a.ActorUserId)).ShouldBe(
            [
                (AuditAction.AppointmentRequested, rejected, parent.UserId),
                (AuditAction.AppointmentRejected, rejected, w.Class.TeacherId),
                (AuditAction.AppointmentRequested, cancelled, parent.UserId),
                (AuditAction.AppointmentApproved, cancelled, w.Class.TeacherId),
                (AuditAction.AppointmentCancelled, cancelled, w.Class.TeacherId),
                (AuditAction.AppointmentRequested, byGuardian, parent.UserId),
                (AuditAction.AppointmentCancelled, byGuardian, parent.UserId),
            ],
            ignoreOrder: true);
        audit.ShouldAllBe(a => a.TargetType == "Appointment" && a.IpAddress == "203.0.113.77" && a.OccurredAt == Morning);
        audit.ShouldAllBe(a => a.Detail!.Contains($"student={gizli:D}") && a.Detail.Contains("status="));
        string auditJson = string.Join('|', await QueryAsync(factory, x => x.Database
            .SqlQuery<string>($"SELECT row_to_json(a)::text AS \"Value\" FROM audit_entries a WHERE a.tenant_id = {w.Class.School.TenantId}")
            .ToListAsync()));
        foreach (string secret in new[] { "Gizlikonu", "Gizlineden", "Gizliöğrenci", parentName })
        {
            auditJson.ShouldNotContain(secret);
        }
    }
}
