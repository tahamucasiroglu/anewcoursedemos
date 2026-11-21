using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Academics.AcademicsTestData;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Academics;

/// <summary>
/// Spec 0013 AC-5: öğretmen atandığı sınıf için ders, Yazılı/Sözlü, başlık ve tarihle değerlendirme açar; değerlendirme sınıfın o
/// andaki öğrencilerini kapsar (hepsi "girilmedi"). Boş alan, 60/100 sınırını aşan alan, geçersiz tür, gelecek tarih ya da
/// sınıfın dönemi dışı tarih alan hatası verir ve hiçbir şey oluşmaz; bugün ve dönemin ilk günü (2026-09-01) kabul edilir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AssessmentCreateTests
{
    private readonly TestDatabase database;

    public AssessmentCreateTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Create_TodayAndTermFirstDay_AcceptedWithClassSnapshot()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        var (status, created, raw) = await CreateAssessmentAsync(client, w.Class.Teacher, w.Class.ClassroomId, Exam(new string('d', 60), "Oral", new string('b', 100), Today));
        status.ShouldBe(HttpStatusCode.OK, raw);
        created.GetProperty("studentCount").GetInt32().ShouldBe(2);
        Guid first = await OpenedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Exam("Türkçe", "Written", "Dönem başı", "2026-09-01"));

        JsonElement list = (await AssessmentsAsync(client, w.Class.Teacher, w.Class.ClassroomId)).Body;
        list.GetArrayLength().ShouldBe(2);
        list[0].GetProperty("kind").GetString().ShouldBe("Oral");
        list[0].GetProperty("date").GetString().ShouldBe(Today);
        list[1].GetProperty("id").GetGuid().ShouldBe(first);
        list[1].GetProperty("kind").GetString().ShouldBe("Written");
        JsonElement grades = (await GradesAsync(client, w.Class.Teacher, first)).Body;
        ByStudent(grades, "score").ShouldAllBe(kv => kv.Value == null);
        ByStudent(grades, "score").Keys.ShouldBe([w.Ali, w.Ayse], ignoreOrder: true);
    }

    [Theory]
    [InlineData("", "Written", "1. yazılı", Today, "Ders adı zorunludur")]
    [InlineData("Türkçe", "Written", "", Today, "Başlık zorunludur")]
    [InlineData("D61", "Written", "1. yazılı", Today, "Ders adı en fazla 60 karakter olabilir")]
    [InlineData("Türkçe", "Written", "B101", Today, "Başlık en fazla 100 karakter olabilir")]
    [InlineData("Türkçe", "Quiz", "1. yazılı", Today, "Tür Yazılı ya da Sözlü olmalı")]
    [InlineData("Türkçe", "1", "1. yazılı", Today, "Tür Yazılı ya da Sözlü olmalı")]
    [InlineData("Türkçe", "Written", "1. yazılı", Tomorrow, "Tarih bugünden sonra olamaz")]
    [InlineData("Türkçe", "Written", "1. yazılı", "2026-08-31", "Tarih sınıfın dönemi içinde olmalı")]
    public async Task Create_InvalidField_400AndNothingCreated(string subject, string kind, string title, string date, string message)
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        subject = subject == "D61" ? new string('d', 61) : subject;
        title = title == "B101" ? new string('b', 101) : title;

        var (status, problem, _) = await CreateAssessmentAsync(client, w.Class.Teacher, w.Class.ClassroomId, Exam(subject, kind, title, date));

        status.ShouldBe(HttpStatusCode.BadRequest);
        Messages(problem).ShouldContain(message);
        (await AssessmentCountAsync(factory, w.Class.ClassroomId)).ShouldBe(0);
    }
}
