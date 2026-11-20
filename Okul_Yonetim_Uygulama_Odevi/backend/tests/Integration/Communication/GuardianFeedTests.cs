using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Tests.Integration.Attendance;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Communication;

/// <summary>
/// Spec 0011 AC-3: veli seçili okulda yalnızca alıcısı olduğu duyuruları yeniden eskiye, sayfa başına 20 görür (farklı
/// şubelerdeki çocuklarınınkiler dahil); başka velinin çocuğuna özel ve başka okulun duyurusu listede yok, doğrudan
/// istendiğinde 404; okunmamışlar işaretli; boşken liste boş; sayfa 1–500 dışı 400 (review L-3); duyuru ve okul bildirimi
/// karışık akışta 2. ve 3. sayfa birebir (review L-4). Sınıf/öğrenci kapsamında ilgili çocuğun adı satırdadır.
/// </summary>
[Trait("Category", "Integration")]
public sealed class GuardianFeedTests
{
    private readonly TestDatabase database;

    public GuardianFeedTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Feed_NewestFirst_PagesOf20_UnreadMarked_ChildNamed()
    {
        FakeTimeProvider time = new(Morning);
        using OkulApiFactory factory = Factory(this.database, time: time);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        List<Guid> ids = [];
        for (int i = 1; i <= 21; i++)
        {
            time.Advance(TimeSpan.FromSeconds(10));
            ids.Add(await PublishedAsync(client, w.Class.Teacher, Plain("Classroom", w.Class.ClassroomId, title: $"Duyuru {i}")));
        }

        (await DetailAsync(client, w.AliMother.Token, ids[20])).Status.ShouldBe(HttpStatusCode.OK);

        JsonElement first = (await FeedAsync(client, w.AliMother.Token)).Body;
        JsonElement second = (await FeedAsync(client, w.AliMother.Token, 2)).Body;

        Titles(first).Length.ShouldBe(20);
        Titles(first)[0].ShouldBe("Duyuru 21");
        Titles(first)[19].ShouldBe("Duyuru 2");
        first.GetProperty("hasMore").GetBoolean().ShouldBeTrue();
        first.GetProperty("unreadCount").GetInt32().ShouldBe(20);
        Titles(second).ShouldBe(["Duyuru 1"]);
        second.GetProperty("hasMore").GetBoolean().ShouldBeFalse();

        JsonElement newest = first.GetProperty("items")[0];
        newest.GetProperty("kind").GetString().ShouldBe("Announcement");
        newest.GetProperty("isRead").GetBoolean().ShouldBeTrue();
        newest.GetProperty("authorName").GetString().ShouldNotBeNullOrEmpty();
        newest.GetProperty("childNames").EnumerateArray().Select(n => n.GetString()).ShouldBe(["Ali Yılmaz"]);
        first.GetProperty("items")[1].GetProperty("isRead").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Feed_20AnnouncementsAnd25SchoolNotices_Pages2And3Exact()
    {
        FakeTimeProvider time = new(Morning);
        using OkulApiFactory factory = Factory(this.database, time: time);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        List<(Guid Id, DateTimeOffset At)> expected = [];
        for (int i = 1; i <= 20; i++)
        {
            time.Advance(TimeSpan.FromSeconds(10));
            expected.Add((await PublishedAsync(client, w.Class.Teacher, Plain("Classroom", w.Class.ClassroomId, title: $"Duyuru {i}")), time.GetUtcNow()));
        }

        // Okul bildirimleri duyuruların arasına düşen zamanlarla (farklı günler — veli + öğrenci + gün tekil).
        GuardianNotice[] notices = [.. Enumerable.Range(1, 25).Select(i => GuardianNotice.Absent(
            w.Class.School.TenantId, w.AliMother.UserId, w.Ali, new DateOnly(2026, 11, 19).AddDays(-i), Morning.AddSeconds((i * 8) + 3)))];
        await WithContextAsync(factory, c =>
        {
            c.GuardianNotices.AddRange(notices);
            return Task.CompletedTask;
        });
        expected.AddRange(notices.Select(n => (n.Id, n.UpdatedAt)));
        Guid[] order = [.. expected.OrderByDescending(e => e.At).ThenByDescending(e => e.Id).Select(e => e.Id)];

        JsonElement[] pages = [.. await Task.WhenAll(Enumerable.Range(1, 3).Select(async p => (await FeedAsync(client, w.AliMother.Token, p)).Body))];
        Guid[] Ids(JsonElement page) => [.. page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid())];

        Ids(pages[0]).ShouldBe(order[..20]);
        Ids(pages[1]).ShouldBe(order[20..40]);
        Ids(pages[2]).ShouldBe(order[40..]);
        pages[1].GetProperty("items").EnumerateArray().Select(i => i.GetProperty("kind").GetString()).Distinct().ShouldBe(["Announcement", "SchoolNotice"], ignoreOrder: true);
        pages.Select(p => p.GetProperty("hasMore").GetBoolean()).ShouldBe([true, true, false]);
        pages[2].GetProperty("page").GetInt32().ShouldBe(3);
    }

    [Fact]
    public async Task Feed_PageOutOfRange_400_NoOverflow()
    {
        using OkulApiFactory factory = CommunicationTestData.Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await PublishedAsync(client, w.Class.Teacher, Plain("Classroom", w.Class.ClassroomId));

        foreach (int page in new[] { 0, -1, 501, int.MaxValue, 107374183 })
        {
            var (status, body, _) = await FeedAsync(client, w.AliMother.Token, page);
            status.ShouldBe(HttpStatusCode.BadRequest, $"sayfa {page}");
            Messages(body).ShouldContain("Sayfa 1 ile 500 arasında olmalı");
        }

        var (lastStatus, last, _) = await FeedAsync(client, w.AliMother.Token, 500);
        lastStatus.ShouldBe(HttpStatusCode.OK);
        last.GetProperty("items").GetArrayLength().ShouldBe(0);
        last.GetProperty("hasMore").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Feed_OtherChildAndOtherSchool_NotListed_DirectRequestNotFound_EmptyWhenNone()
    {
        using OkulApiFactory factory = CommunicationTestData.Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid forAyse = await PublishedAsync(client, w.Class.Teacher, Plain("Student", w.Ayse, title: "Ayşe'ye özel"));
        Guid forAli = await PublishedAsync(client, w.Class.Teacher, Plain("Student", w.Ali, title: "Ali'ye özel"));
        AttendanceClass other = await ClassAsync(factory, client);
        Guid otherStudent = await StudentAsync(client, other, "Başka Okul");
        await ParentAsync(client, other, otherStudent);
        Guid otherSchool = await PublishedAsync(client, other.Founder, Plain("School", null, title: "Başka okul duyurusu"));

        Titles((await FeedAsync(client, w.AliMother.Token)).Body).ShouldBe(["Ali'ye özel"]);
        (await DetailAsync(client, w.AliMother.Token, forAyse)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await DetailAsync(client, w.AliMother.Token, otherSchool)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await DetailAsync(client, w.AliMother.Token, forAli)).Status.ShouldBe(HttpStatusCode.OK);

        JsonElement empty = (await FeedAsync(client, w.BerkMother.Token)).Body;
        empty.GetProperty("items").GetArrayLength().ShouldBe(0);
        empty.GetProperty("unreadCount").GetInt32().ShouldBe(0);
        (await FeedAsync(client, w.Class.Teacher)).Status.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Feed_ChildrenInDifferentBranches_BothBranchesIncluded()
    {
        using OkulApiFactory factory = CommunicationTestData.Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid avcilar = await CreateBranchAsync(client, w.Class.Founder, "Avcılar");
        Guid sixA = await CreateClassroomAsync(client, w.Class.Founder, w.Class.TermId, avcilar, w.Class.LevelId, "6-A");
        (Guid teacherId, _, string teacher) = await TeacherAsync(factory, client, w.Class.School.TenantId, avcilar);
        await AssignAsync(client, w.Class.Founder, sixA, teacherId);
        Guid deniz = await CreateStudentAsync(client, w.Class.Founder, NewStudent(avcilar, "Deniz Yılmaz", classroomId: sixA));
        await LinkAsync(client, w.Class.Founder, deniz, Guardian(w.AliMother.Email));

        await PublishedAsync(client, w.Class.Teacher, Plain("Classroom", w.Class.ClassroomId, title: "Merkez 5-A"));
        await PublishedAsync(client, teacher, Plain("Classroom", sixA, title: "Avcılar 6-A"));

        string[] titles = Titles((await FeedAsync(client, w.AliMother.Token)).Body);
        titles.ShouldBe(["Merkez 5-A", "Avcılar 6-A"], ignoreOrder: true);
        Titles((await FeedAsync(client, w.AyseMother.Token)).Body).ShouldBe(["Merkez 5-A"]);
    }
}
