using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Communication;

/// <summary>
/// Spec 0011 AC-5: "Onay iste" ile soru (zorunlu, en fazla 300) ve son gün (yarından en geç 180 gün sonrasına) girilir;
/// geçmiş/bugün/181. gün alan hatası verir ve hiçbir şey oluşmaz; yayınlayan ve kurucu öğrenci bazında onay / ret / cevapsız
/// sayılarını ve listesini görür. Bugün (Türkiye günü) 2026-11-20.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ConsentFormTests
{
    private readonly TestDatabase database;

    public ConsentFormTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Publish_ConsentFieldErrors_NothingCreated_LimitsAccepted()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid classroom = w.Class.ClassroomId;
        string teacher = w.Class.Teacher;

        var (noQuestion, noQuestionBody, _) = await PublishAsync(client, teacher, Form("Classroom", classroom, "2026-11-27", question: " "));
        var (longQuestion, longQuestionBody, _) = await PublishAsync(client, teacher, Form("Classroom", classroom, "2026-11-27", question: new string('s', 301)));
        var (noDeadline, noDeadlineBody, _) = await PublishAsync(client, teacher, new { title = "Gezi", body = "Metin", scope = "Classroom", scopeId = classroom, requestConsent = true, consentQuestion = "Katılsın mı?" });
        noQuestion.ShouldBe(HttpStatusCode.BadRequest);
        Messages(noQuestionBody).ShouldContain("Onay sorusu zorunludur");
        longQuestion.ShouldBe(HttpStatusCode.BadRequest);
        Messages(longQuestionBody).ShouldContain("Onay sorusu en fazla 300 karakter olabilir");
        noDeadline.ShouldBe(HttpStatusCode.BadRequest);
        Messages(noDeadlineBody).ShouldContain("Son tarih zorunludur");

        foreach (string bad in new[] { "2026-11-19", "2026-11-20", "2027-05-20" })
        {
            var (status, body, _) = await PublishAsync(client, teacher, Form("Classroom", classroom, bad));
            status.ShouldBe(HttpStatusCode.BadRequest, bad);
            Messages(body).ShouldContain("Son tarih yarından en geç 180 gün sonrasına kadar olabilir");
        }

        (await AnnouncementCountAsync(factory, w.Class.School.TenantId)).ShouldBe(0);
        (await PublishAsync(client, teacher, Form("Classroom", classroom, "2026-11-21", question: new string('s', 300)))).Status.ShouldBe(HttpStatusCode.OK);
        (await PublishAsync(client, teacher, Form("Classroom", classroom, "2027-05-19"))).Status.ShouldBe(HttpStatusCode.OK);
        (await AnnouncementCountAsync(factory, w.Class.School.TenantId)).ShouldBe(2);
    }

    [Fact]
    public async Task Results_PerStudentApprovedDeclinedPending_ForAuthorAndFounder()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid cem = await Attendance.AttendanceTestData.StudentAsync(client, w.Class, "Cem Ak");
        ParentAccount cemMother = await ParentAsync(client, w.Class, cem);
        Guid id = await PublishedAsync(client, w.Class.Teacher, Form("Classroom", w.Class.ClassroomId, "2026-11-27"));

        (await AnswerAsync(client, w.AliFather.Token, id, w.Ali, "Approved")).Status.ShouldBe(HttpStatusCode.OK);
        (await AnswerAsync(client, cemMother.Token, id, cem, "Declined")).Status.ShouldBe(HttpStatusCode.OK);

        foreach (string viewer in new[] { w.Class.Teacher, w.Class.Founder })
        {
            JsonElement consent = (await ResultsAsync(client, viewer, id)).Body.GetProperty("consent");
            consent.GetProperty("approved").GetInt32().ShouldBe(1);
            consent.GetProperty("declined").GetInt32().ShouldBe(1);
            consent.GetProperty("pending").GetInt32().ShouldBe(1);
            JsonElement[] students = [.. consent.GetProperty("students").EnumerateArray()];
            students.Select(s => s.GetProperty("studentName").GetString()).ShouldBe(["Ali Yılmaz", "Ayşe Kaya", "Cem Ak"]);
            students[0].GetProperty("answer").GetString().ShouldBe("Approved");
            students[0].GetProperty("answeredByName").GetString().ShouldBe("Veli Kişi");
            students[1].GetProperty("answer").ValueKind.ShouldBe(JsonValueKind.Null);
            students[2].GetProperty("answer").GetString().ShouldBe("Declined");
        }
    }
}
