using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Tests.Integration.Account;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Attendance;

/// <summary>
/// Spec 0010 AC-4: Var/Yok/Geç tek seferde kaydedilir; sonuç sayıları (var, yok, geç, bildirim giden veli, velisi kayıtlı
/// olmayan devamsız öğrenci) doğrudur; aynı gün yeniden açınca kayıtlı durumlar ve kayıt saati (Türkiye saati) gelir; aynı
/// gün yeniden kaydetmek yoklamayı günceller (tek yoklama kalır, saat yenilenir). Geçersiz durum ve tekrar eden öğrenci reddedilir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SaveAttendanceTests
{
    private readonly TestDatabase database;

    public SaveAttendanceTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Save_CountsAreRight_ReopenShowsSavedStatusesAndTime_ResaveUpdates()
    {
        FakeTimeProvider time = new(Morning);
        using OkulApiFactory factory = Factory(this.database, new CapturingOutbox(), time);
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);
        Guid ali = await StudentAsync(client, c, "Ali Yılmaz");
        Guid ayse = await StudentAsync(client, c, "Ayşe Kaya");
        Guid berk = await StudentAsync(client, c, "Berk Demir");
        Guid cem = await StudentAsync(client, c, "Cem Öz");
        await NewGuardianAsync(client, c.Founder, c.School.TenantId, ali);
        await NewGuardianAsync(client, c.Founder, c.School.TenantId, ali, "Father");
        Guid[] all = [ali, ayse, berk, cem];

        JsonElement result = await SavedAsync(client, c.Teacher, c.ClassroomId, Marks(all, (ali, "Absent"), (ayse, "Absent"), (berk, "Late")));

        result.GetProperty("present").GetInt32().ShouldBe(1);
        result.GetProperty("absent").GetInt32().ShouldBe(2);
        result.GetProperty("late").GetInt32().ShouldBe(1);
        result.GetProperty("notifiedGuardians").GetInt32().ShouldBe(2, "Ali'nin iki velisi");
        result.GetProperty("studentsWithoutGuardian").GetInt32().ShouldBe(1, "Ayşe'nin velisi yok");
        result.GetProperty("savedTime").GetString().ShouldBe("12:00", "09:00 UTC = 12:00 Türkiye");

        var (_, view, _) = await TodayAsync(client, c.Teacher, c.ClassroomId);
        view.GetProperty("isSaved").GetBoolean().ShouldBeTrue();
        view.GetProperty("savedTime").GetString().ShouldBe("12:00");
        Statuses(view).ShouldBe(new Dictionary<Guid, string> { [ali] = "Absent", [ayse] = "Absent", [berk] = "Late", [cem] = "Present" });

        time.Advance(TimeSpan.FromMinutes(10));
        JsonElement again = await SavedAsync(client, c.Teacher, c.ClassroomId, Marks(all, (ali, "Absent"), (ayse, "Present"), (berk, "Late")));
        again.GetProperty("absent").GetInt32().ShouldBe(1);
        again.GetProperty("present").GetInt32().ShouldBe(2);
        again.GetProperty("notifiedGuardians").GetInt32().ShouldBe(0, "Ali zaten bildirildi; Ayşe'nin velisi yok");
        again.GetProperty("studentsWithoutGuardian").GetInt32().ShouldBe(0);
        again.GetProperty("savedTime").GetString().ShouldBe("12:10");
        var (_, reopened, _) = await TodayAsync(client, c.Teacher, c.ClassroomId);
        reopened.GetProperty("savedTime").GetString().ShouldBe("12:10");
        Statuses(reopened)[ayse].ShouldBe("Present");
        (await SheetCountAsync(factory, c.ClassroomId)).ShouldBe(1);
    }

    [Fact]
    public async Task Save_InvalidStatusOrRepeatedStudent_IsRejected_NothingSaved()
    {
        using OkulApiFactory factory = Factory(this.database, new CapturingOutbox());
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);
        Guid ali = await StudentAsync(client, c, "Ali Yılmaz");

        var (badStatus, badBody, _) = await SaveAsync(client, c.Teacher, c.ClassroomId, [(ali, "Excused")]);
        badStatus.ShouldBe(HttpStatusCode.BadRequest);
        Messages(badBody).ShouldContain("Durum Var, Yok ya da Geç olmalı");
        var (twice, twiceBody, _) = await SaveAsync(client, c.Teacher, c.ClassroomId, [(ali, "Absent"), (ali, "Present")]);
        twice.ShouldBe(HttpStatusCode.BadRequest);
        Messages(twiceBody).ShouldContain("Bir öğrenci listede bir kez yer alır");
        (await SheetCountAsync(factory, c.ClassroomId)).ShouldBe(0);
    }

    private static Dictionary<Guid, string> Statuses(JsonElement view) =>
        view.GetProperty("students").EnumerateArray().ToDictionary(s => s.GetProperty("studentId").GetGuid(), s => s.GetProperty("status").GetString()!);
}
