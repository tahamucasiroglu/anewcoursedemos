using System.Net;
using Microsoft.EntityFrameworkCore;
using OkulYonetim.Domain.Ai;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Ai.WeeklySummaryTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Spec 0014 AC-11 (gizlilik ve audit; S2, S6): üretim ve gönderimden sonra log satırlarında özet (model taslağı ve son metin),
/// öğretmen notu, dayanak satırı ve öğrenci adı geçmez — yalnızca kimlikler ve sayılar; gönderim audit kaydı oluşturur (kim, hangi
/// taslak, ayrıntıda öğrenci + hafta + veli sayısı, ne zaman, IP) ve audit'te de metin yoktur.
/// </summary>
[Trait("Category", "Integration")]
public sealed class WeeklySummaryPrivacyAuditTests
{
    private readonly TestDatabase database;

    public WeeklySummaryPrivacyAuditTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task LogsHoldOnlyIds_SendAudited()
    {
        FakeAiTextGenerator text = new() { Reply = "Gizliözet bu hafta çok çalıştı." };
        using OkulApiFactory factory = Factory(this.database, text);
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.77");
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid gizli = await StudentAsync(client, w.Class, "Gizliöğrenci Adsoyad");
        await ParentAsync(client, w.Class, gizli);
        await SavedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Marks([w.Ali, w.Ayse, gizli], (gizli, "Absent")));

        await GeneratedAsync(client, w.Class.Teacher, gizli, "Gizlinot resim dersinde çok başarılı.");
        (await SendSummaryAsync(client, w.Class.Teacher, gizli, "Gizlison metin onaylandı.")).Status.ShouldBe(HttpStatusCode.OK);

        string log = factory.ReadLog();
        foreach (string secret in new[] { "Gizliözet", "Gizlinot", "Gizlison", "Gizliöğrenci", "Adsoyad", "Dayanak:", "gün gelmedi", "Haftalık gelişim özeti —" })
        {
            log.ShouldNotContain(secret, customMessage: secret);
        }

        AiDraft draft = (await SummaryDraftsAsync(factory, w.Class.School.TenantId)).ShouldHaveSingleItem();
        string[] lines = log.Split('\n');
        lines.Where(l => l.Contains("Haftalık özet taslağı oluşturuldu", StringComparison.Ordinal)).ShouldHaveSingleItem().ShouldContain(gizli.ToString());
        lines.Where(l => l.Contains("Haftalık özet gönderildi", StringComparison.Ordinal)).ShouldHaveSingleItem().ShouldContain(draft.Id.ToString());

        AuditEntry audit = (await AuditAsync(factory, w.Class.School.TenantId, AuditAction.WeeklySummarySent)).ShouldHaveSingleItem();
        audit.ActorUserId.ShouldBe(w.Class.TeacherId);
        audit.TargetType.ShouldBe("AiDraft");
        audit.TargetId.ShouldBe(draft.Id);
        audit.Detail.ShouldBe($"student={gizli:D};week=2026-11-16;guardians=1");
        audit.IpAddress.ShouldBe("203.0.113.77");
        audit.OccurredAt.ShouldBe(Morning);
        string auditJson = string.Join('|', await QueryAsync(factory, x => x.Database
            .SqlQuery<string>($"SELECT row_to_json(a)::text AS \"Value\" FROM audit_entries a WHERE a.tenant_id = {w.Class.School.TenantId}")
            .ToListAsync()));
        auditJson.ShouldNotContain("Gizli");
    }
}
