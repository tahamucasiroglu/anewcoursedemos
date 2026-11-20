using System.Net;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Communication;

/// <summary>
/// Spec 0011 AC-2: öğretmen yalnızca atandığı sınıfa ve o sınıfın öğrencisine yayınlar; okul/şube kapsamı 403, başka sınıf ya
/// da başka sınıfın öğrencisi 404 — hiçbir şey oluşmaz; veli ve muhasebe 403; girişsiz istek 401.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AnnouncementAuthorizationTests
{
    private readonly TestDatabase database;

    public AnnouncementAuthorizationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Teacher_OwnClassroomAndStudentOnly_OthersRejectedNothingCreated()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        string teacher = w.Class.Teacher;

        (await PublishAsync(client, teacher, Plain("Classroom", w.Class.ClassroomId))).Status.ShouldBe(HttpStatusCode.OK);
        (await PublishAsync(client, teacher, Plain("Student", w.Ali))).Status.ShouldBe(HttpStatusCode.OK);
        int created = await AnnouncementCountAsync(factory, w.Class.School.TenantId);

        var (schoolStatus, school, _) = await PublishAsync(client, teacher, Plain("School", null));
        schoolStatus.ShouldBe(HttpStatusCode.Forbidden);
        Detail(school).ShouldBe("Öğretmen yalnızca kendi sınıflarına ve o sınıflardaki öğrencilere duyuru yayınlayabilir.");
        (await PublishAsync(client, teacher, Plain("Branch", w.Class.School.BranchId))).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await PublishAsync(client, teacher, Plain("Classroom", w.OtherClassroomId))).Status.ShouldBe(HttpStatusCode.NotFound);
        (await PublishAsync(client, teacher, Plain("Student", w.Berk))).Status.ShouldBe(HttpStatusCode.NotFound);
        (await PublishAsync(client, teacher, Plain("Classroom", Guid.NewGuid()))).Status.ShouldBe(HttpStatusCode.NotFound);

        (await AnnouncementCountAsync(factory, w.Class.School.TenantId)).ShouldBe(created);
        created.ShouldBe(2);
    }

    [Fact]
    public async Task GuardianAccountingAndAnonymous_Rejected()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        string accountantEmail = UniqueEmail("muhasebe");
        await AddMemberAsync(factory, accountantEmail, w.Class.School.TenantId, Role.Accounting, w.Class.School.BranchId);
        string accountant = await TokenForSchoolAsync(client, accountantEmail, w.Class.School.TenantId);

        (await PublishAsync(client, w.AliMother.Token, Plain("Classroom", w.Class.ClassroomId))).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await PublishAsync(client, accountant, Plain("Classroom", w.Class.ClassroomId))).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await StatusAsync(client, HttpMethod.Get, "/api/announcements", w.AliMother.Token)).ShouldBe(HttpStatusCode.Forbidden);
        (await StatusAsync(client, HttpMethod.Get, "/api/announcements", accountant)).ShouldBe(HttpStatusCode.Forbidden);

        using HttpResponseMessage anonymous = await PostJsonAsync(client, "/api/announcements", Plain("School", null));
        anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await AnnouncementCountAsync(factory, w.Class.School.TenantId)).ShouldBe(0);
    }

    [Fact]
    public async Task StaffList_FounderSeesAll_TeacherSeesOwn()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await PublishedAsync(client, w.Class.Founder, Plain("School", null, title: "Kurucudan"));
        await PublishedAsync(client, w.Class.Teacher, Plain("Classroom", w.Class.ClassroomId, title: "5-A öğretmeninden"));
        await PublishedAsync(client, w.OtherTeacher, Plain("Classroom", w.OtherClassroomId, title: "5-B öğretmeninden"));

        var founderList = (await CallAsync(client, HttpMethod.Get, "/api/announcements", w.Class.Founder)).Body.EnumerateArray().Select(a => a.GetProperty("title").GetString()).ToArray();
        var teacherList = (await CallAsync(client, HttpMethod.Get, "/api/announcements", w.Class.Teacher)).Body.EnumerateArray().ToArray();

        founderList.ShouldBe(["5-B öğretmeninden", "5-A öğretmeninden", "Kurucudan"], ignoreOrder: true);
        teacherList.ShouldHaveSingleItem().GetProperty("title").GetString().ShouldBe("5-A öğretmeninden");
        teacherList[0].GetProperty("recipientCount").GetInt32().ShouldBe(3);
        teacherList[0].GetProperty("readCount").GetInt32().ShouldBe(0);
    }
}
