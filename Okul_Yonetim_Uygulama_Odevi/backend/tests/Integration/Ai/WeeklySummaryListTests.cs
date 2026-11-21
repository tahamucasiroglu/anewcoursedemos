using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Ai.WeeklySummaryTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Spec 0014 AC-1: öğretmen yalnızca atandığı sınıfları görür ("Haftalık özet" bağlantısı için <c>/api/me</c>'de
/// <c>teachesClassroom</c> yalnızca sınıf ataması olan öğretmende); seçilen sınıf ve haftada öğrenciler Türkçe ad sırasıyla
/// "Özet yok" / "Taslak hazır" / "Gönderildi" durumlarıyla gelir; "Geçen hafta" bir önceki Pazartesi–Pazar'ı kullanır ve o haftanın
/// durumları bu haftadan ayrıdır.
/// </summary>
[Trait("Category", "Integration")]
public sealed class WeeklySummaryListTests
{
    private readonly TestDatabase database;

    public WeeklySummaryListTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Classrooms_OnlyAssigned_MeFlagOnlyForAssignedTeacher()
    {
        using OkulApiFactory factory = Factory(this.database, new FakeAiTextGenerator());
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (_, _, string unassigned) = await TeacherAsync(factory, client, w.Class.School.TenantId, w.Class.School.BranchId);

        var (status, mine, raw) = await ClassroomsAsync(client, w.Class.Teacher);
        status.ShouldBe(HttpStatusCode.OK, raw);
        mine.EnumerateArray().Select(c => c.GetProperty("id").GetGuid()).ShouldBe([w.Class.ClassroomId]);
        mine[0].GetProperty("name").GetString().ShouldBe("5-A");
        (await ClassroomsAsync(client, w.OtherTeacher)).Body.EnumerateArray().Select(c => c.GetProperty("id").GetGuid()).ShouldBe([w.OtherClassroomId]);
        (await ClassroomsAsync(client, unassigned)).Body.GetArrayLength().ShouldBe(0);

        (await MeAsync(client, w.Class.Teacher)).GetProperty("teachesClassroom").GetBoolean().ShouldBeTrue();
        (await MeAsync(client, unassigned)).GetProperty("teachesClassroom").GetBoolean().ShouldBeFalse("atanmış sınıf yok");
        (await MeAsync(client, w.Class.Founder)).GetProperty("teachesClassroom").GetBoolean().ShouldBeFalse();
        (await MeAsync(client, w.AliMother.Token)).GetProperty("teachesClassroom").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task List_TurkishOrderAndStatuses_PreviousWeekSeparate()
    {
        FakeAiTextGenerator text = new() { Reply = "Bu hafta güzel geçti." };
        using OkulApiFactory factory = Factory(this.database, text);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid cinar = await StudentAsync(client, w.Class, "Çınar Ak");
        Guid cem = await StudentAsync(client, w.Class, "Cem Er");
        await SavedAsync(client, w.Class.Teacher, w.Class.ClassroomId, Marks([w.Ali, w.Ayse, cinar, cem]));
        await GeneratedAsync(client, w.Class.Teacher, w.Ali);
        await GeneratedAsync(client, w.Class.Teacher, w.Ayse);
        (await SendSummaryAsync(client, w.Class.Teacher, w.Ayse, "Ayşe bu hafta her gün okuldaydı.")).Status.ShouldBe(HttpStatusCode.OK);

        var (status, list, raw) = await ListAsync(client, w.Class.Teacher, w.Class.ClassroomId);

        status.ShouldBe(HttpStatusCode.OK, raw);
        list.GetProperty("weekStart").GetString().ShouldBe("2026-11-16");
        list.GetProperty("weekEnd").GetString().ShouldBe("2026-11-22");
        list.GetProperty("weekLabel").GetString().ShouldBe(ThisWeekLabel);
        list.GetProperty("classroomName").GetString().ShouldBe("5-A");
        list.GetProperty("students").EnumerateArray().Select(s => s.GetProperty("fullName").GetString()).ShouldBe(["Ali Yılmaz", "Ayşe Kaya", "Cem Er", "Çınar Ak"]);
        Statuses(list).ShouldBe(["Draft", "Sent", "None", "None"]);

        var (previousStatus, previous, previousRaw) = await ListAsync(client, w.Class.Teacher, w.Class.ClassroomId, "previous");
        previousStatus.ShouldBe(HttpStatusCode.OK, previousRaw);
        previous.GetProperty("weekStart").GetString().ShouldBe("2026-11-09");
        previous.GetProperty("weekEnd").GetString().ShouldBe("2026-11-15");
        previous.GetProperty("weekLabel").GetString().ShouldBe("9–15 Kasım");
        Statuses(previous).ShouldBe(["None", "None", "None", "None"], "geçen haftanın özeti yok");

        (await ListAsync(client, w.Class.Teacher, w.Class.ClassroomId, "next")).Status.ShouldBe(HttpStatusCode.BadRequest);
        (await PanelAsync(client, w.Class.Teacher, w.Ali, "previous")).Body.GetProperty("status").GetString().ShouldBe("None");
    }

    [Fact]
    public async Task List_EmptyClassroom_NoStudents()
    {
        using OkulApiFactory factory = Factory(this.database, new FakeAiTextGenerator());
        using HttpClient client = factory.CreateClient();
        Attendance.AttendanceClass c = await ClassAsync(factory, client);

        var (status, list, raw) = await ListAsync(client, c.Teacher, c.ClassroomId);

        status.ShouldBe(HttpStatusCode.OK, raw);
        list.GetProperty("students").GetArrayLength().ShouldBe(0);
        list.GetProperty("students").ValueKind.ShouldBe(JsonValueKind.Array);
    }
}
