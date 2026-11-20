using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Communication;

/// <summary>
/// Spec 0011 AC-1: kurucu okul, şube, sınıf ve öğrenci kapsamında yayınlar; alıcılar yayın anındaki kapsamın okulda etkin
/// velileridir (pasif veli ve sonradan sınıfa eklenen öğrencinin velisi almaz); kapsamda veli yoksa reddedilir; boş ya da
/// sınırı aşan başlık/metin alan hatası verir ve hiçbir şey oluşmaz (150/4000 kabul).
/// </summary>
[Trait("Category", "Integration")]
public sealed class AnnouncementPublishTests
{
    private readonly TestDatabase database;

    public AnnouncementPublishTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Publish_FounderEveryScope_RecipientsAreActiveGuardiansInScope()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        string founder = w.Class.Founder;

        (await PublishAsync(client, founder, Plain("School", null))).Body.GetProperty("recipientCount").GetInt32().ShouldBe(4);
        (await PublishAsync(client, founder, Plain("Branch", w.Class.School.BranchId))).Body.GetProperty("recipientCount").GetInt32().ShouldBe(4);
        (await PublishAsync(client, founder, Plain("Classroom", w.Class.ClassroomId))).Body.GetProperty("recipientCount").GetInt32().ShouldBe(3);
        (await PublishAsync(client, founder, Plain("Student", w.Ali))).Body.GetProperty("recipientCount").GetInt32().ShouldBe(2);

        (await StatusAsync(client, HttpMethod.Post, $"/api/admin/members/{w.AliFather.UserId}/deactivate", founder)).ShouldBe(HttpStatusCode.OK);
        (await PublishAsync(client, founder, Plain("Student", w.Ali, title: "Pasif baba almaz"))).Body.GetProperty("recipientCount").GetInt32().ShouldBe(1);

        (await FeedAsync(client, w.BerkMother.Token)).Body.GetProperty("items").GetArrayLength().ShouldBe(2, "okul ve şube duyurusu; 5-A ve Ali'ye özel olanlar yok");
        (await FeedAsync(client, w.AyseMother.Token)).Body.GetProperty("items").GetArrayLength().ShouldBe(3, "okul, şube, 5-A");
    }

    [Fact]
    public async Task Publish_RecipientsFixedAtPublishTime_LaterStudentGuardianDoesNotSee()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid announcement = await PublishedAsync(client, w.Class.Founder, Plain("Classroom", w.Class.ClassroomId));

        Guid cem = await StudentAsync(client, w.Class, "Cem Ak");
        ParentAccount cemMother = await ParentAsync(client, w.Class, cem);

        (await FeedAsync(client, cemMother.Token)).Body.GetProperty("items").GetArrayLength().ShouldBe(0);
        (await DetailAsync(client, cemMother.Token, announcement)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await ResultsAsync(client, w.Class.Founder, announcement)).Body.GetProperty("recipientCount").GetInt32().ShouldBe(3);
    }

    [Fact]
    public async Task Publish_ScopeWithoutGuardians_RejectedNothingCreated()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid alone = await StudentAsync(client, w.Class, "Velisiz Öğrenci");

        var (status, body, _) = await PublishAsync(client, w.Class.Founder, Plain("Student", alone));

        status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(body).ShouldBe("Bu kapsamda duyuru alacak veli yok.");
        (await AnnouncementCountAsync(factory, w.Class.School.TenantId)).ShouldBe(0);
    }

    [Fact]
    public async Task Publish_FieldErrors_NothingCreated_LimitsAccepted()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        string founder = w.Class.Founder;
        Guid classroom = w.Class.ClassroomId;

        var (emptyStatus, empty, _) = await PublishAsync(client, founder, Plain("Classroom", classroom, title: "  ", body: ""));
        var (longStatus, tooLong, _) = await PublishAsync(client, founder, Plain("Classroom", classroom, title: new string('a', 151), body: new string('b', 4001)));
        var (scopeStatus, scope, _) = await PublishAsync(client, founder, Plain("Galaxy", classroom));

        emptyStatus.ShouldBe(HttpStatusCode.BadRequest);
        Messages(empty).ShouldContain("Başlık zorunludur");
        Messages(empty).ShouldContain("Metin zorunludur");
        longStatus.ShouldBe(HttpStatusCode.BadRequest);
        Messages(tooLong).ShouldContain("Başlık en fazla 150 karakter olabilir");
        Messages(tooLong).ShouldContain("Metin en fazla 4000 karakter olabilir");
        scopeStatus.ShouldBe(HttpStatusCode.BadRequest);
        Messages(scope).ShouldContain("Kapsam okul, şube, sınıf ya da öğrenci olmalı");
        (await AnnouncementCountAsync(factory, w.Class.School.TenantId)).ShouldBe(0);

        var (okStatus, ok, _) = await PublishAsync(client, founder, Plain("Classroom", classroom, title: new string('a', 150), body: new string('b', 4000)));
        okStatus.ShouldBe(HttpStatusCode.OK);
        JsonElement detail = (await DetailAsync(client, founder, ok.GetProperty("id").GetGuid())).Body;
        detail.GetProperty("title").GetString()!.Length.ShouldBe(150);
        detail.GetProperty("body").GetString()!.Length.ShouldBe(4000);
        (await AnnouncementCountAsync(factory, w.Class.School.TenantId)).ShouldBe(1);
    }
}
