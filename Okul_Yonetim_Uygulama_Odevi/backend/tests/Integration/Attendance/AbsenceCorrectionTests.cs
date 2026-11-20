using System.Text.Json;
using OkulYonetim.Application.Ports;
using OkulYonetim.Tests.Integration.Account;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Attendance;

/// <summary>
/// Spec 0010 AC-8: aynı gün yeniden kaydetmek aynı öğrenci için ikinci "yok" bildirimi/e-postası üretmez; "Yok" → "Var" ve
/// "Yok" → "Geç" velilere düzeltme bildirimi ve e-postası gönderir, bildirim "düzeltildi" (Corrected) görünür ve yeniden
/// okunmamış olur; yeniden "Yok" yeni bir "yok" bildirimi ve e-postası gönderir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AbsenceCorrectionTests
{
    private readonly TestDatabase database;

    public AbsenceCorrectionTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Resave_NoDuplicate_Correction_ThenAbsentAgain()
    {
        CapturingOutbox outbox = new();
        using OkulApiFactory factory = Factory(this.database, outbox);
        using HttpClient client = factory.CreateClient();
        AttendanceClass c = await ClassAsync(factory, client);
        Guid ali = await StudentAsync(client, c, "Ali Yılmaz");
        Guid ayse = await StudentAsync(client, c, "Ayşe Kaya");
        (string mother, _, string motherEmail, _) = await NewGuardianAsync(client, c.Founder, c.School.TenantId, ali);
        (string ayseMother, _, string ayseEmail, _) = await NewGuardianAsync(client, c.Founder, c.School.TenantId, ayse);
        Guid[] all = [ali, ayse];

        await SavedAsync(client, c.Teacher, c.ClassroomId, Marks(all, (ali, "Absent"), (ayse, "Absent")));
        JsonElement same = await SavedAsync(client, c.Teacher, c.ClassroomId, Marks(all, (ali, "Absent"), (ayse, "Absent")));
        same.GetProperty("notifiedGuardians").GetInt32().ShouldBe(0);
        outbox.To(motherEmail).Length.ShouldBe(1, "ikinci kayıt yeni e-posta üretmez");
        (await NoticeCountAsync(factory, ali)).ShouldBe(1);
        (await StatusAsync(client, HttpMethod.Post, "/api/notices/read", mother)).ShouldBe(System.Net.HttpStatusCode.NoContent);
        (await UnreadAsync(client, mother)).ShouldBe(0);

        JsonElement corrected = await SavedAsync(client, c.Teacher, c.ClassroomId, Marks(all, (ali, "Present"), (ayse, "Late")));
        corrected.GetProperty("notifiedGuardians").GetInt32().ShouldBe(2);
        EmailMessage correction = outbox.To(motherEmail).Last();
        correction.Subject.ShouldBe($"{c.School.SchoolName} — yoklama düzeltmesi");
        correction.Body.ShouldStartWith("Ali Yılmaz için bugünkü yoklama düzeltildi: okulda.");
        outbox.To(ayseEmail).Last().Body.ShouldStartWith("Ayşe Kaya için bugünkü yoklama düzeltildi: okulda.", customMessage: "Yok → Geç de düzeltmedir");
        JsonElement notice = (await NoticesAsync(client, mother)).Body.EnumerateArray().ShouldHaveSingleItem();
        notice.GetProperty("kind").GetString().ShouldBe("Corrected");
        notice.GetProperty("text").GetString().ShouldBe("Ali Yılmaz için bugünkü yoklama düzeltildi: okulda.");
        notice.GetProperty("isRead").GetBoolean().ShouldBeFalse("düzeltme yeniden okunmamış");
        (await UnreadAsync(client, mother)).ShouldBe(1);
        (await UnreadAsync(client, ayseMother)).ShouldBe(1);

        await SavedAsync(client, c.Teacher, c.ClassroomId, Marks(all, (ali, "Present"), (ayse, "Late")));
        outbox.To(motherEmail).Length.ShouldBe(2, "düzeltme de bir kez");

        JsonElement again = await SavedAsync(client, c.Teacher, c.ClassroomId, Marks(all, (ali, "Absent"), (ayse, "Late")));
        again.GetProperty("notifiedGuardians").GetInt32().ShouldBe(1);
        outbox.To(motherEmail).Length.ShouldBe(3);
        outbox.To(motherEmail).Last().Subject.ShouldBe($"{c.School.SchoolName} — devamsızlık bildirimi");
        (await NoticesAsync(client, mother)).Body.EnumerateArray().ShouldHaveSingleItem().GetProperty("kind").GetString().ShouldBe("Absent");
        (await NoticeCountAsync(factory, ali)).ShouldBe(1, "veli + öğrenci + gün tekil");
    }
}
