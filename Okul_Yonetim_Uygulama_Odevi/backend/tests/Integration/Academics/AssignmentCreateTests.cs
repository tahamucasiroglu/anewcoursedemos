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
/// Spec 0013 AC-1: öğretmen atandığı sınıfa ders, başlık, açıklama ve teslim tarihiyle ödev verir, ödev listede görünür; ödevin
/// öğrencileri verildiği andaki sınıf öğrencileridir, hepsi "İşaretlenmedi" (sonradan gelen öğrenci yoktur). Boş ders/başlık,
/// 60/150/2000 sınırını aşan alan, dünkü ya da 91 gün sonraki teslim tarihi alan hatası verir ve hiçbir şey oluşmaz; sınırdaki
/// 60/150/2000, bugün ve 90. gün kabul edilir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AssignmentCreateTests
{
    private readonly TestDatabase database;

    public AssignmentCreateTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Create_ValidHomework_ListedAndStudentsSnapshotUnmarked()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        var (status, created, raw) = await CreateAssignmentAsync(client, w.Class.Teacher, w.Class.ClassroomId, Homework());
        status.ShouldBe(HttpStatusCode.OK, raw);
        created.GetProperty("studentCount").GetInt32().ShouldBe(2);
        Guid id = created.GetProperty("id").GetGuid();
        Guid late = await StudentAsync(client, w.Class, "Can Sonradan");

        JsonElement list = (await AssignmentsAsync(client, w.Class.Teacher, w.Class.ClassroomId)).Body;
        list.GetArrayLength().ShouldBe(1);
        JsonElement row = list[0];
        row.GetProperty("id").GetGuid().ShouldBe(id);
        row.GetProperty("subject").GetString().ShouldBe("Matematik");
        row.GetProperty("title").GetString().ShouldBe("Kesirler alıştırması");
        row.GetProperty("dueDate").GetString().ShouldBe("2026-11-25");
        row.GetProperty("studentCount").GetInt32().ShouldBe(2, "sonradan gelen öğrenci ödevde yok");

        JsonElement submissions = (await SubmissionsAsync(client, w.Class.Teacher, id)).Body;
        Dictionary<Guid, string?> statuses = ByStudent(submissions, "status");
        statuses.Keys.ShouldBe([w.Ali, w.Ayse], ignoreOrder: true);
        statuses.Values.ShouldAllBe(s => s == "Unmarked");
        statuses.ContainsKey(late).ShouldBeFalse();
    }

    [Fact]
    public async Task Create_AtLimits_TodayAndDay90_Accepted()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        (await CreateAssignmentAsync(client, w.Class.Teacher, w.Class.ClassroomId, Homework(new string('m', 60), new string('b', 150), new string('a', 2000), Today)))
            .Status.ShouldBe(HttpStatusCode.OK);
        (await CreateAssignmentAsync(client, w.Class.Teacher, w.Class.ClassroomId, Homework(dueDate: "2027-02-18", description: null)))
            .Status.ShouldBe(HttpStatusCode.OK, "90. gün");
        (await AssignmentCountAsync(factory, w.Class.ClassroomId)).ShouldBe(2);
    }

    [Theory]
    [InlineData("", "Başlık", "2026-11-25", "Ders adı zorunludur")]
    [InlineData("   ", "Başlık", "2026-11-25", "Ders adı zorunludur")]
    [InlineData("Matematik", "", "2026-11-25", "Başlık zorunludur")]
    [InlineData("M61", "Başlık", "2026-11-25", "Ders adı en fazla 60 karakter olabilir")]
    [InlineData("Matematik", "T151", "2026-11-25", "Başlık en fazla 150 karakter olabilir")]
    [InlineData("Matematik", "Başlık", "2026-11-19", "Teslim tarihi bugün ile 90 gün sonrası arasında olmalı")]
    [InlineData("Matematik", "Başlık", "2027-02-19", "Teslim tarihi bugün ile 90 gün sonrası arasında olmalı")]
    public async Task Create_InvalidField_400AndNothingCreated(string subject, string title, string due, string message)
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        subject = subject == "M61" ? new string('m', 61) : subject;
        title = title == "T151" ? new string('b', 151) : title;

        var (status, problem, _) = await CreateAssignmentAsync(client, w.Class.Teacher, w.Class.ClassroomId, Homework(subject, title, null, due));

        status.ShouldBe(HttpStatusCode.BadRequest);
        Messages(problem).ShouldContain(message);
        (await AssignmentCountAsync(factory, w.Class.ClassroomId)).ShouldBe(0);
    }

    [Fact]
    public async Task Create_DescriptionOver2000OrMissingDue_400()
    {
        using OkulApiFactory factory = Factory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        var (tooLong, problem, _) = await CreateAssignmentAsync(client, w.Class.Teacher, w.Class.ClassroomId, Homework(description: new string('a', 2001)));
        var (noDue, problem2, _) = await CreateAssignmentAsync(client, w.Class.Teacher, w.Class.ClassroomId, new { subject = "Matematik", title = "Başlık" });

        tooLong.ShouldBe(HttpStatusCode.BadRequest);
        Messages(problem).ShouldContain("Açıklama en fazla 2000 karakter olabilir");
        noDue.ShouldBe(HttpStatusCode.BadRequest);
        Messages(problem2).ShouldContain("Teslim tarihi zorunludur");
        (await AssignmentCountAsync(factory, w.Class.ClassroomId)).ShouldBe(0);
    }
}
