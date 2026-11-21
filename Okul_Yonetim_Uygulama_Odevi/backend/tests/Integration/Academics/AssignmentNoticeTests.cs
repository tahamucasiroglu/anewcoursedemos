using System.Net;
using System.Text.Json;
using OkulYonetim.Application.Ports;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Academics.AcademicsTestData;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Academics;

/// <summary>
/// Spec 0013 AC-2: ödev kaydedilince ödevin her öğrencisinin okulda etkin her velisine bildirim kapısından bir "Okul bildirimi"
/// geçer; metin "Yeni ödev — {ders}: {başlık}. Teslim: {gün.ay.yıl}." (açıklama yok) ve o velinin duyuru akışında yalnızca o
/// çocuk için görünür; pasif veliye ve başka sınıfın velisine gitmez; gönderici hata verirse ödev yine kaydedilir. Aynı gün
/// devamsızlık bildirimiyle çakışmaz (yoklama bildiriminin gün tekilliği yalnızca kendisi için).
/// </summary>
[Trait("Category", "Integration")]
public sealed class AssignmentNoticeTests
{
    private const string Expected = "Yeni ödev — Matematik: Kesirler alıştırması. Teslim: 25.11.2026.";
    private readonly TestDatabase database;

    public AssignmentNoticeTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Create_NotifiesEachActiveGuardianOfEachStudent_FeedShowsTextForThatChild()
    {
        CapturingPushNotifier push = new();
        using OkulApiFactory factory = Factory(this.database, push);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/members/{w.AliFather.UserId}/deactivate", w.Class.Founder)).ShouldBe(HttpStatusCode.OK);

        var (status, created, _) = await CreateAssignmentAsync(client, w.Class.Teacher, w.Class.ClassroomId, Homework());

        status.ShouldBe(HttpStatusCode.OK);
        created.GetProperty("notifiedGuardians").GetInt32().ShouldBe(2);
        push.To(w.AliMother.UserId).Length.ShouldBe(1);
        push.To(w.AyseMother.UserId).Length.ShouldBe(1);
        push.To(w.AliFather.UserId).ShouldBeEmpty("pasif veli");
        push.To(w.BerkMother.UserId).ShouldBeEmpty("başka sınıfın velisi");
        push.Sent.ShouldAllBe(p => p.Kind == PushNotificationKind.SchoolNotice);

        JsonElement feed = (await FeedAsync(client, w.AliMother.Token)).Body;
        JsonElement notice = feed.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("kind").GetString() == "SchoolNotice");
        notice.GetProperty("title").GetString().ShouldBe(Expected);
        notice.GetProperty("title").GetString()!.ShouldNotContain("Sayfa 42", customMessage: "açıklama bildirimde yok");
        notice.GetProperty("childNames").EnumerateArray().Select(c => c.GetString()).ShouldBe(["Ali Yılmaz"]);
        Titles((await FeedAsync(client, w.BerkMother.Token)).Body).ShouldNotContain(Expected);
    }

    [Fact]
    public async Task Create_SenderFails_AssignmentStillSaved_WarningLogged()
    {
        ThrowingPushNotifier failing = new();
        using OkulApiFactory factory = Factory(this.database, failing);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        var (status, created, _) = await CreateAssignmentAsync(client, w.Class.Teacher, w.Class.ClassroomId, Homework());

        status.ShouldBe(HttpStatusCode.OK);
        failing.Attempts.ShouldBe(3);
        (await AssignmentCountAsync(factory, w.Class.ClassroomId)).ShouldBe(1);
        (await AssignmentsAsync(client, w.Class.Teacher, w.Class.ClassroomId)).Body[0].GetProperty("id").GetGuid().ShouldBe(created.GetProperty("id").GetGuid());
        factory.ReadLog().ShouldContain("Bildirim gönderilemedi");
        Titles((await FeedAsync(client, w.AliMother.Token)).Body).ShouldContain(Expected);
    }

    [Fact]
    public async Task SameDay_AbsenceAndTwoAssignments_AllNoticesKept()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        await AssignedAsync(client, w.Class.Teacher, w.Class.ClassroomId);
        await AssignedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Homework("Fen", "Deney raporu"));
        await SavedAsync(client, w.Class.Teacher, w.Class.ClassroomId, [(w.Ali, "Absent"), (w.Ayse, "Present")]);

        JsonElement notices = (await NoticesAsync(client, w.AliMother.Token)).Body;
        notices.EnumerateArray().Select(n => n.GetProperty("kind").GetString()).ShouldBe(["Absent", "Assignment", "Assignment"], ignoreOrder: true);
        (await UnreadAsync(client, w.AliMother.Token)).ShouldBe(3);
    }
}
