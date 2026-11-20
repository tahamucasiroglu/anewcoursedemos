using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Tests.Integration.Attendance;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Communication;

/// <summary>
/// Spec 0011 gereksinim 1 / AC-12, review M-1: duyuru formunun kapsam seçenekleri yayının kabul ettiği kümeyle aynıdır —
/// öğretmen okul günü olmayan bir günde (hafta sonu tatili) de kendi sınıfını ve o sınıfın öğrencilerini seçebilir; 26 öğrencili sınıfın
/// 26. öğrencisi de listededir (sayfasız); öğretmen başka sınıfın öğrencilerini soramaz (404), veli 403, başka okul 404.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AnnouncementScopeOptionsTests
{
    private static readonly DateTimeOffset Saturday = new(2026, 11, 21, 9, 0, 0, TimeSpan.Zero);

    private readonly TestDatabase database;

    public AnnouncementScopeOptionsTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task ScopeOptions_TeacherOnNonSchoolDay_OwnClassroomAndItsStudents_PublishAccepted()
    {
        using OkulApiFactory factory = Factory(this.database, time: new FakeTimeProvider(Saturday));
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (await CallAsync(client, HttpMethod.Post, $"/api/admin/terms/{w.Class.TermId}/calendar-entries", w.Class.Founder, new { title = "Hafta sonu tatili", kind = "Holiday", startDate = "2026-11-21", endDate = "2026-11-22" }))
            .Status.ShouldBe(HttpStatusCode.OK);
        (await TodayAsync(client, w.Class.Teacher, w.Class.ClassroomId)).Body.GetProperty("isSchoolDay").GetBoolean().ShouldBeFalse("cumartesi tatil: okul günü değil");

        var (classroomsStatus, classrooms, _) = await ScopeClassroomsAsync(client, w.Class.Teacher);
        classroomsStatus.ShouldBe(HttpStatusCode.OK);
        classrooms.EnumerateArray().Select(c => c.GetProperty("id").GetGuid()).ShouldBe([w.Class.ClassroomId]);
        classrooms[0].GetProperty("name").GetString().ShouldBe("5-A");

        var (studentsStatus, students, _) = await ScopeStudentsAsync(client, w.Class.Teacher, w.Class.ClassroomId);
        studentsStatus.ShouldBe(HttpStatusCode.OK);
        students.EnumerateArray().Select(s => s.GetProperty("fullName").GetString()).ShouldBe(["Ali Yılmaz", "Ayşe Kaya"]);
        (await PublishAsync(client, w.Class.Teacher, Plain("Student", students[1].GetProperty("id").GetGuid()))).Status.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ScopeStudents_ClassOf26_AllListed_26thSelectable()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid last = Guid.Empty;
        for (int i = 3; i <= 26; i++)
        {
            last = await StudentAsync(client, w.Class, $"Öğrenci {i:00}");
        }

        await ParentAsync(client, w.Class, last);
        foreach (string token in new[] { w.Class.Founder, w.Class.Teacher })
        {
            JsonElement students = (await ScopeStudentsAsync(client, token, w.Class.ClassroomId)).Body;
            students.GetArrayLength().ShouldBe(26);
            students.EnumerateArray().Select(s => s.GetProperty("id").GetGuid()).ShouldContain(last);
        }

        (await PublishAsync(client, w.Class.Teacher, Plain("Student", last))).Status.ShouldBe(HttpStatusCode.OK);
        (await ScopeClassroomsAsync(client, w.Class.Founder)).Body.EnumerateArray().Select(c => c.GetProperty("id").GetGuid())
            .ShouldBe([w.Class.ClassroomId, w.OtherClassroomId], ignoreOrder: true);
    }

    [Fact]
    public async Task ScopeStudents_OtherClassroomGuardianOrOtherSchool_Rejected()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        AttendanceClass other = await ClassAsync(factory, client);

        (await ScopeStudentsAsync(client, w.Class.Teacher, w.OtherClassroomId)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await ScopeStudentsAsync(client, w.Class.Teacher, other.ClassroomId)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await ScopeStudentsAsync(client, w.Class.Founder, other.ClassroomId)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await ScopeStudentsAsync(client, w.AliMother.Token, w.Class.ClassroomId)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await ScopeClassroomsAsync(client, w.AliMother.Token)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await ScopeClassroomsAsync(client, other.Founder)).Body.EnumerateArray().Select(c => c.GetProperty("id").GetGuid()).ShouldBe([other.ClassroomId]);
        (await ScopeStudentsAsync(client, w.Class.Founder, w.OtherClassroomId)).Body.EnumerateArray().Select(s => s.GetProperty("fullName").GetString()).ShouldBe(["Berk Demir"]);
    }

    private static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> ScopeClassroomsAsync(HttpClient client, string token) =>
        CallAsync(client, HttpMethod.Get, "/api/announcements/scope-classrooms", token);

    private static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> ScopeStudentsAsync(HttpClient client, string token, Guid classroomId) =>
        CallAsync(client, HttpMethod.Get, $"/api/announcements/scope-students?classroomId={classroomId}", token);
}
