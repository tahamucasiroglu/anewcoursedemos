using System.Net;
using OkulYonetim.Tests.Integration.Attendance;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Communication;

/// <summary>
/// Spec 0011 AC-11 (izolasyon): başka okulun kurucusu, öğretmeni ve velisi A okulunun duyurusunu, sonuçlarını, onay kaydını ve
/// konuşmasını göremez/değiştiremez (404); aynı okulda yetkisiz şubedeki öğretmen başka şubenin duyurusunu ve sonuçlarını
/// göremez (404).
/// </summary>
[Trait("Category", "Integration")]
public sealed class CommunicationIsolationTests
{
    private readonly TestDatabase database;

    public CommunicationIsolationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task OtherSchool_AnnouncementConsentAndConversation_NotFound()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid form = await PublishedAsync(client, w.Class.Founder, Form("Classroom", w.Class.ClassroomId, "2026-11-27"));
        (await AnswerAsync(client, w.AliMother.Token, form, w.Ali, "Approved")).Status.ShouldBe(HttpStatusCode.OK);
        Guid conversation = await StartedAsync(client, w.AliMother.Token, w.Ali, w.Class.TeacherId);
        (await SendAsync(client, w.AliMother.Token, conversation, "Merhaba")).Status.ShouldBe(HttpStatusCode.OK);

        AttendanceClass b = await ClassAsync(factory, client);
        Guid bStudent = await StudentAsync(client, b, "B Okulu Öğrencisi");
        ParentAccount bGuardian = await ParentAsync(client, b, bStudent);

        foreach (string outsider in new[] { b.Founder, b.Teacher, bGuardian.Token })
        {
            (await DetailAsync(client, outsider, form)).Status.ShouldBe(HttpStatusCode.NotFound);
            (await MessagesAsync(client, outsider, conversation)).Status.ShouldBe(HttpStatusCode.NotFound);
            (await SendAsync(client, outsider, conversation, "sızma")).Status.ShouldBe(HttpStatusCode.NotFound);
        }

        (await ResultsAsync(client, b.Founder, form)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await ResultsAsync(client, b.Teacher, form)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await AnswerAsync(client, bGuardian.Token, form, w.Ali, "Declined")).Status.ShouldBe(HttpStatusCode.NotFound);
        (await AnswerAsync(client, bGuardian.Token, form, bStudent, "Declined")).Status.ShouldBe(HttpStatusCode.NotFound);
        (await StartAsync(client, bGuardian.Token, w.Ali, w.Class.TeacherId)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await PublishAsync(client, b.Founder, Plain("Classroom", w.Class.ClassroomId))).Status.ShouldBe(HttpStatusCode.NotFound);
        (await PublishAsync(client, b.Founder, Plain("Student", w.Ali))).Status.ShouldBe(HttpStatusCode.NotFound);
        (await CallAsync(client, HttpMethod.Get, "/api/announcements", b.Founder)).Body.GetArrayLength().ShouldBe(0);
        (await ConversationsAsync(client, b.Teacher)).Body.GetArrayLength().ShouldBe(0);

        (await ResultsAsync(client, w.Class.Founder, form)).Body.GetProperty("consent").GetProperty("approved").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task OtherBranchTeacher_CannotSeeAnnouncementOrResults()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid avcilar = await CreateBranchAsync(client, w.Class.Founder, "Avcılar");
        (_, _, string avcilarTeacher) = await TeacherAsync(factory, client, w.Class.School.TenantId, avcilar);
        Guid id = await PublishedAsync(client, w.Class.Teacher, Plain("Classroom", w.Class.ClassroomId));

        (await DetailAsync(client, avcilarTeacher, id)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await ResultsAsync(client, avcilarTeacher, id)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await PublishAsync(client, avcilarTeacher, Plain("Classroom", w.Class.ClassroomId))).Status.ShouldBe(HttpStatusCode.NotFound);
        (await CallAsync(client, HttpMethod.Get, "/api/announcements", avcilarTeacher)).Body.GetArrayLength().ShouldBe(0);
    }
}
