using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Academics.AcademicsTestData;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Ai.WeeklySummaryTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Spec 0014 AC-2 (gerçek kayıtlarla, sahte saat ileri alınarak): dayanak o haftanın yoklama sayılarını (alınan gün, var/yok/geç,
/// yok ve geç günleri), teslim günü o haftada olan ödevleri (durumuyla) ve günü o haftada olan notları (puanıyla) içerir; Türkiye saatiyle
/// Pazartesi 00:00'da alınan yoklama dahil, önceki Pazar 23:59'da alınan ve sonraki Pazartesi'nin kayıtları hariç; aynı sınıftaki başka
/// öğrencinin verisi (Ayşe'nin yoklaması ve puanı) yoktur; öğretmen notu dayanakta durur.
/// </summary>
[Trait("Category", "Integration")]
public sealed class WeeklySummarySourceTests
{
    private readonly TestDatabase database;

    public WeeklySummarySourceTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Basis_WeekBoundariesInTurkeyTime_OnlyThisStudentsRecords()
    {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 15, 20, 59, 0, TimeSpan.Zero));
        FakeAiTextGenerator text = new();
        using OkulApiFactory factory = Factory(this.database, text, time: time);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        Guid tenant = w.Class.School.TenantId;

        // Pazar 23:59 (Türkiye) — önceki hafta.
        await SaveDayAsync(client, w, w.Class.Teacher, "2026-11-15", (w.Ali, "Absent"), (w.Ayse, "Absent"));

        // Pazartesi 00:00 (Türkiye) = Pazar 21:00 UTC — bu hafta.
        time.SetUtcNow(new DateTimeOffset(2026, 11, 15, 21, 0, 0, TimeSpan.Zero));
        await SaveDayAsync(client, w, w.Class.Teacher, "2026-11-16", (w.Ali, "Late"), (w.Ayse, "Absent"));

        time.SetUtcNow(new DateTimeOffset(2026, 11, 18, 9, 0, 0, TimeSpan.Zero));
        string teacher = await TokenForSchoolAsync(client, w.Class.TeacherEmail, tenant);
        await SaveDayAsync(client, w, teacher, "2026-11-18", (w.Ali, "Absent"));
        Guid math = await AssignedAsync(client, teacher, w.Class.ClassroomId, Homework("Matematik", "Kesirler", null, "2026-11-20"));
        (await SaveSubmissionsAsync(client, teacher, math, (w.Ali, "Submitted"), (w.Ayse, "NotSubmitted"))).Status.ShouldBe(HttpStatusCode.OK);
        await AssignedAsync(client, teacher, w.Class.ClassroomId, Homework("Fen", "Gelecek hafta", null, "2026-11-23"));
        Guid exam = await OpenedAsync(client, teacher, w.Class.ClassroomId, Exam("Türkçe", "Written", "1. yazılı", "2026-11-18"));
        (await SaveGradesAsync(client, teacher, exam, (w.Ali, 85), (w.Ayse, 40))).Status.ShouldBe(HttpStatusCode.OK);
        Guid oldExam = await OpenedAsync(client, teacher, w.Class.ClassroomId, Exam("Tarih", "Oral", "Önceki hafta", "2026-11-15"));
        (await SaveGradesAsync(client, teacher, oldExam, (w.Ali, 70))).Status.ShouldBe(HttpStatusCode.OK);

        // Sonraki Pazartesi 00:00 (Türkiye) — "geçen hafta" artık 16–22 Kasım.
        time.SetUtcNow(new DateTimeOffset(2026, 11, 22, 21, 0, 0, TimeSpan.Zero));
        teacher = await TokenForSchoolAsync(client, w.Class.TeacherEmail, tenant);
        await SaveDayAsync(client, w, teacher, "2026-11-23", (w.Ali, "Present"));

        var (status, panel, raw) = await PanelAsync(client, teacher, w.Ali, "previous");

        status.ShouldBe(HttpStatusCode.OK, raw);
        panel.GetProperty("weekStart").GetString().ShouldBe("2026-11-16");
        panel.GetProperty("weekLabel").GetString().ShouldBe(ThisWeekLabel);
        panel.GetProperty("status").GetString().ShouldBe("None");
        panel.GetProperty("hasData").GetBoolean().ShouldBeTrue();
        JsonElement basis = panel.GetProperty("basis");
        basis.GetProperty("firstName").GetString().ShouldBe("Ali");
        JsonElement attendance = basis.GetProperty("attendance");
        attendance.GetProperty("daysTaken").GetInt32().ShouldBe(2, "Pazar 23:59 ve sonraki Pazartesi hariç");
        attendance.GetProperty("present").GetInt32().ShouldBe(0);
        attendance.GetProperty("late").GetInt32().ShouldBe(1);
        attendance.GetProperty("absent").GetInt32().ShouldBe(1, "Ayşe'nin devamsızlıkları sayılmaz");
        Days(attendance, "absentDays").ShouldBe(["2026-11-18"]);
        Days(attendance, "lateDays").ShouldBe(["2026-11-16"], "Pazartesi 00:00 (Türkiye) kaydı dahil");
        JsonElement assignment = basis.GetProperty("assignments").EnumerateArray().ShouldHaveSingleItem();
        assignment.GetProperty("subject").GetString().ShouldBe("Matematik");
        assignment.GetProperty("status").GetString().ShouldBe("Submitted");
        JsonElement grade = basis.GetProperty("grades").EnumerateArray().ShouldHaveSingleItem();
        grade.GetProperty("subject").GetString().ShouldBe("Türkçe");
        grade.GetProperty("score").GetInt32().ShouldBe(85);
        panel.GetProperty("basisLine").GetString().ShouldBe("Dayanak: 1 gün geldi (1 gün geç), 1 gün gelmedi; 1 ödev; 1 not");

        var (_, current, _) = await PanelAsync(client, teacher, w.Ali);
        current.GetProperty("weekStart").GetString().ShouldBe("2026-11-23");
        current.GetProperty("basis").GetProperty("attendance").GetProperty("daysTaken").GetInt32().ShouldBe(1);
        current.GetProperty("basis").GetProperty("attendance").GetProperty("present").GetInt32().ShouldBe(1);
        current.GetProperty("basis").GetProperty("assignments").EnumerateArray().ShouldHaveSingleItem().GetProperty("subject").GetString().ShouldBe("Fen");
        current.GetProperty("basis").GetProperty("grades").GetArrayLength().ShouldBe(0);

        JsonElement drafted = await GeneratedAsync(client, teacher, w.Ali, "Derse katılımı arttı.", "previous");
        drafted.GetProperty("basis").GetProperty("teacherNote").GetString().ShouldBe("Derse katılımı arttı.");
        drafted.GetProperty("basisLine").GetString().ShouldBe("Dayanak: 1 gün geldi (1 gün geç), 1 gün gelmedi; 1 ödev; 1 not; öğretmen notu");
        string content = text.Requests.ShouldHaveSingleItem().Content;
        content.ShouldContain("Gelmediği günler: 18 Kasım Çarşamba");
        content.ShouldContain("Geç geldiği günler: 16 Kasım Pazartesi");
        content.ShouldNotContain("15 Kasım");
        content.ShouldNotContain("Gelecek hafta");
        content.ShouldNotContain("Önceki hafta");
        content.ShouldNotContain("40 puan");
        (await PanelAsync(client, teacher, w.Ali, "previous")).Body.GetProperty("basis").GetProperty("teacherNote").GetString()
            .ShouldBe("Derse katılımı arttı.", "öğretmen notu taslağın dayanağında saklanır");
    }

    private static string[] Days(JsonElement attendance, string property) =>
        [.. attendance.GetProperty(property).EnumerateArray().Select(d => d.GetString()!)];

    private static async Task SaveDayAsync(HttpClient client, CommunicationWorld w, string teacher, string day, params (Guid StudentId, string Status)[] marks)
    {
        var (status, _, raw) = await SaveAsync(client, teacher, w.Class.ClassroomId, Marks([w.Ali, w.Ayse], marks), day);
        status.ShouldBe(HttpStatusCode.OK, raw);
    }
}
