using System.Net;
using System.Text.Json;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Communication;

/// <summary>
/// Spec 0011 AC-11 (audit): duyuru yayınlama, onay cevabı (verme ve değiştirme) ve mesai saatleri değişikliği audit kaydı
/// oluşturur — kim, ne, hangi kayıt, ne zaman, IP; ayrıntıda metin yok. Tek tek mesajlar audit'e yazılmaz.
/// </summary>
[Trait("Category", "Integration")]
public sealed class CommunicationAuditTests
{
    private const string Ip = "203.0.113.77";
    private readonly TestDatabase database;

    public CommunicationAuditTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task PublishConsentAndQuietHours_Audited_MessagesNot()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", Ip);
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid tenant = w.Class.School.TenantId;

        Guid form = await PublishedAsync(client, w.Class.Teacher, Form("Classroom", w.Class.ClassroomId, "2026-11-27", title: "Gizlibaşlık"));
        (await AnswerAsync(client, w.AliMother.Token, form, w.Ali, "Approved")).Status.ShouldBe(HttpStatusCode.OK);
        (await AnswerAsync(client, w.AliFather.Token, form, w.Ali, "Declined")).Status.ShouldBe(HttpStatusCode.OK);
        (await SetQuietAsync(client, w.Class.Teacher, "08:00", "17:00", "Monday")).Status.ShouldBe(HttpStatusCode.OK);
        Guid conversation = await StartedAsync(client, w.AliMother.Token, w.Ali, w.Class.TeacherId);
        (await SendAsync(client, w.AliMother.Token, conversation, "Gizlimesaj")).Status.ShouldBe(HttpStatusCode.OK);

        AuditEntry published = (await AuditAsync(factory, tenant, AuditAction.AnnouncementPublished)).ShouldHaveSingleItem();
        published.ActorUserId.ShouldBe(w.Class.TeacherId);
        published.TargetType.ShouldBe("Announcement");
        published.TargetId.ShouldBe(form);
        published.IpAddress.ShouldBe(Ip);
        published.OccurredAt.ShouldBe(Morning);
        published.Detail.ShouldBe("scope=Classroom;consent=True;recipients=3;students=2");

        List<AuditEntry> answers = await AuditAsync(factory, tenant, AuditAction.ConsentAnswered);
        answers.Count.ShouldBe(2);
        answers.ShouldAllBe(a => a.TargetType == "Announcement" && a.TargetId == form && a.IpAddress == Ip);
        answers.Single(a => a.ActorUserId == w.AliMother.UserId).Detail.ShouldBe($"student={w.Ali:D};answer=Approved;changed=False");
        answers.Single(a => a.ActorUserId == w.AliFather.UserId).Detail.ShouldBe($"student={w.Ali:D};answer=Declined;changed=True");

        AuditEntry quiet = (await AuditAsync(factory, tenant, AuditAction.QuietHoursChanged)).ShouldHaveSingleItem();
        quiet.ActorUserId.ShouldBe(w.Class.TeacherId);
        quiet.TargetType.ShouldBe("QuietHours");
        quiet.IpAddress.ShouldBe(Ip);
        quiet.Detail.ShouldBe("enabled=true;start=08:00;end=17:00;days=1");

        string all = JsonSerializer.Serialize(await AuditAsync(factory, tenant, Enum.GetValues<AuditAction>()));
        all.ShouldNotContain("Gizlibaşlık");
        all.ShouldNotContain("Gizlimesaj");
    }
}
