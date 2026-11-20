using System.Net;
using System.Text.Json;
using OkulYonetim.Application.Ports;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Communication;

/// <summary>
/// Spec 0011 AC-10: duyuru/onay formu yayınlanınca her alıcı veliye, öğretmen mesajında veliye birer bildirim kapıdan geçer;
/// yoklamanın (spec 0010) kapıya verdiği öğrenci bildirimi o öğrencinin velilerinin akışında "Okul bildirimi" olarak görünür
/// ve başka veliye görünmez; gönderici hata verirse duyuru/mesaj yine kaydedilir ve Warning log yazılır.
/// </summary>
[Trait("Category", "Integration")]
public sealed class NotificationGateTests
{
    private readonly TestDatabase database;

    public NotificationGateTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task PublishAndTeacherMessage_OneNotificationPerRecipient()
    {
        CapturingPushNotifier push = new();
        using OkulApiFactory factory = Factory(this.database, push);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        Guid plain = await PublishedAsync(client, w.Class.Teacher, Plain("Classroom", w.Class.ClassroomId));
        Guid form = await PublishedAsync(client, w.Class.Founder, Form("Student", w.Ali, "2026-11-27"));
        Guid id = await StartedAsync(client, w.Class.Teacher, w.Ali, w.AliMother.UserId);
        (await SendAsync(client, w.Class.Teacher, id, "Ali bugün çok iyiydi")).Status.ShouldBe(HttpStatusCode.OK);

        PushNotification[] announcement = [.. push.Sent.Where(p => p.SourceId == plain)];
        announcement.Select(p => p.RecipientUserId).ShouldBe([w.AliMother.UserId, w.AliFather.UserId, w.AyseMother.UserId], ignoreOrder: true);
        announcement.ShouldAllBe(p => p.Kind == PushNotificationKind.Announcement && p.TenantId == w.Class.School.TenantId);
        PushNotification[] consent = [.. push.Sent.Where(p => p.SourceId == form)];
        consent.Select(p => p.RecipientUserId).ShouldBe([w.AliMother.UserId, w.AliFather.UserId], ignoreOrder: true);
        consent.ShouldAllBe(p => p.Kind == PushNotificationKind.ConsentForm);
        push.Sent.Where(p => p.Kind == PushNotificationKind.Message).ShouldHaveSingleItem().RecipientUserId.ShouldBe(w.AliMother.UserId);
        push.To(w.BerkMother.UserId).ShouldBeEmpty();
    }

    [Fact]
    public async Task AttendanceNotice_ThroughGate_ShownAsSchoolNoticeToThatStudentsGuardiansOnly()
    {
        CapturingPushNotifier push = new();
        using OkulApiFactory factory = Factory(this.database, push);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        await SavedAsync(client, w.Class.Teacher, w.Class.ClassroomId, [(w.Ali, "Absent"), (w.Ayse, "Present")]);

        PushNotification[] notices = [.. push.Sent.Where(p => p.Kind == PushNotificationKind.SchoolNotice)];
        notices.Select(p => p.RecipientUserId).ShouldBe([w.AliMother.UserId, w.AliFather.UserId], ignoreOrder: true);
        JsonElement item = (await FeedAsync(client, w.AliMother.Token)).Body.GetProperty("items").EnumerateArray().ShouldHaveSingleItem();
        item.GetProperty("kind").GetString().ShouldBe("SchoolNotice");
        item.GetProperty("authorName").ValueKind.ShouldBe(JsonValueKind.Null);
        item.GetProperty("childNames").EnumerateArray().Select(n => n.GetString()).ShouldBe(["Ali Yılmaz"]);
        item.GetProperty("title").GetString()!.ShouldStartWith("Ali Yılmaz bugün (20.11.2026)");
        item.GetProperty("isRead").GetBoolean().ShouldBeFalse();
        notices.ShouldContain(p => p.SourceId == item.GetProperty("id").GetGuid());
        (await FeedAsync(client, w.AyseMother.Token)).Body.GetProperty("items").GetArrayLength().ShouldBe(0);
        (await FeedAsync(client, w.BerkMother.Token)).Body.GetProperty("items").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task SenderFails_AnnouncementAndMessageStillSaved_WarningLogged()
    {
        ThrowingPushNotifier failing = new();
        using OkulApiFactory factory = Factory(this.database, failing);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        var (status, published, _) = await PublishAsync(client, w.Class.Teacher, Plain("Classroom", w.Class.ClassroomId));
        status.ShouldBe(HttpStatusCode.OK);
        Guid announcement = published.GetProperty("id").GetGuid();
        Guid id = await StartedAsync(client, w.Class.Teacher, w.Ali, w.AliMother.UserId);
        (await SendAsync(client, w.Class.Teacher, id, "Merhaba")).Status.ShouldBe(HttpStatusCode.OK);
        (await SavedAsync(client, w.Class.Teacher, w.Class.ClassroomId, [(w.Ali, "Absent")])).GetProperty("notifiedGuardians").GetInt32().ShouldBe(2);

        failing.Attempts.ShouldBe(3 + 1 + 2);
        (await AnnouncementCountAsync(factory, w.Class.School.TenantId)).ShouldBe(1);
        (await DetailAsync(client, w.AliMother.Token, announcement)).Status.ShouldBe(HttpStatusCode.OK);
        (await MessagesAsync(client, w.AliMother.Token, id)).Body.GetProperty("messages").GetArrayLength().ShouldBe(1);
        (await FeedAsync(client, w.AliMother.Token)).Body.GetProperty("items").GetArrayLength().ShouldBe(2, "duyuru + okul bildirimi");
        string log = factory.ReadLog();
        string warning = log.Split('\n').First(l => l.Contains("Bildirim gönderilemedi", StringComparison.Ordinal) && l.Contains(announcement.ToString(), StringComparison.Ordinal) && l.Contains(w.AliMother.UserId.ToString(), StringComparison.Ordinal));
        warning.ShouldContain("[WRN]");
        warning.ShouldContain("HttpRequestException");
        warning.ShouldContain("tür \"Announcement\"");
        warning.ShouldContain($"kişi \"{w.AliMother.UserId}\"");
        log.ShouldContain("tür \"Message\"");
        log.ShouldContain("tür \"SchoolNotice\"");
    }
}
