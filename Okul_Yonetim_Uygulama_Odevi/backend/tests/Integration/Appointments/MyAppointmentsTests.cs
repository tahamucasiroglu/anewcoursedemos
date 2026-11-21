using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Appointments.AppointmentsTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Appointments;

/// <summary>
/// Spec 0016 AC-10: veli bütün çocuklarının (öteki velinin talep ettikleri dahil, "talep eden" ile) yaklaşan randevularını en yakın üstte
/// ve son 30 günün geçmiş randevularını (en yeni üstte) öğretmen, çocuk, gün-saat ve doğru durumla (Onay bekliyor / Onaylandı /
/// Reddedildi + neden / İptal edildi / Süresi doldu) görür; 31 gün önceki randevu görünmez; iptal düğmesi yalnızca talep eden velide açık
/// randevuda; boşken iki liste de boş ("Henüz randevunuz yok" metni istemcide); başka ailenin randevusu görünmez.
/// </summary>
[Trait("Category", "Integration")]
public sealed class MyAppointmentsTests
{
    private readonly TestDatabase database;

    public MyAppointmentsTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Mine_Empty_BothListsEmpty()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        var (status, body, _) = await MineAsync(client, w.AyseMother.Token);

        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("upcoming").GetArrayLength().ShouldBe(0);
        body.GetProperty("past").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Mine_AllStatesBothParentsOrderingAndThirtyDayWindow()
    {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 10, 19, 9, 0, 0, TimeSpan.Zero));
        using OkulApiFactory factory = AppointmentsFactory(this.database, time: time);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        string teacherName = (await TeachersAsync(client, w.AliMother.Token, w.Ali)).Body[0].GetProperty("fullName").GetString()!;
        string fatherName = await Identity.IdentityTestData.QueryAsync(factory, c => Task.FromResult(c.Users.Single(u => u.Id == w.AliFather.UserId).FullName));

        // Geçmiş: 20 Ekim (31 gün önce — görünmez), 22 Ekim onaylı (babanın talebi), 23 Ekim yanıtlanmamış (süresi doldu).
        foreach (string day in new[] { "2026-10-20", "2026-10-22", "2026-10-23" })
        {
            await OpenedAsync(client, w.Class.Teacher, day: day, start: "14:00", end: "14:30", durationMinutes: 30);
        }

        Guid[] past = await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali);
        Guid old = await RequestedAsync(client, w.AliMother.Token, past[0], w.Ali);
        (await RespondAsync(client, w.Class.Teacher, old, "Approve")).Status.ShouldBe(HttpStatusCode.OK);
        (string teacher, string mother, string father) = await AdvanceAsync(time, new DateTimeOffset(2026, 10, 21, 9, 0, 0, TimeSpan.Zero), client, w);
        Guid approvedPast = await RequestedAsync(client, father, past[1], w.Ali);
        (await RespondAsync(client, teacher, approvedPast, "Approve")).Status.ShouldBe(HttpStatusCode.OK);
        (teacher, mother, father) = await AdvanceAsync(time, new DateTimeOffset(2026, 10, 23, 9, 0, 0, TimeSpan.Zero), client, w);
        Guid expired = await RequestedAsync(client, mother, past[2], w.Ali);

        // Bugün 20 Kasım: yaklaşan dört randevu.
        (teacher, mother, father) = await AdvanceAsync(time, new DateTimeOffset(2026, 11, 20, 9, 0, 0, TimeSpan.Zero), client, w);
        await OpenedAsync(client, teacher, start: "14:00", end: "16:00", durationMinutes: 30);
        Guid[] up = await FreeSlotIdsAsync(client, mother, w.Class.TeacherId, w.Ali);
        Guid rejected = await RequestedAsync(client, mother, up[0], w.Ali);
        (await RespondAsync(client, teacher, rejected, "Reject", "Toplantım var")).Status.ShouldBe(HttpStatusCode.OK);
        Guid teacherCancelled = await RequestedAsync(client, father, up[1], w.Ali);
        (await RespondAsync(client, teacher, teacherCancelled, "Approve")).Status.ShouldBe(HttpStatusCode.OK);
        (await CancelAsync(client, teacher, teacherCancelled, "Hastayım")).Status.ShouldBe(HttpStatusCode.OK);
        Guid guardianCancelled = await RequestedAsync(client, mother, up[2], w.Ali);
        (await CancelAsync(client, mother, guardianCancelled)).Status.ShouldBe(HttpStatusCode.OK);
        Guid pending = await RequestedAsync(client, father, up[3], w.Ali);

        JsonElement mine = (await MineAsync(client, mother)).Body;

        JsonElement[] upcoming = Rows(mine, "upcoming");
        upcoming.Select(r => r.GetProperty("id").GetGuid()).ShouldBe([rejected, teacherCancelled, guardianCancelled, pending], "en yakın üstte");
        upcoming.Select(r => r.GetProperty("state").GetString()).ShouldBe(["Rejected", "CancelledByTeacher", "CancelledByGuardian", "Pending"]);
        upcoming[0].GetProperty("reason").GetString().ShouldBe("Toplantım var");
        upcoming[1].GetProperty("reason").GetString().ShouldBe("Hastayım");
        upcoming[0].GetProperty("startsAt").GetDateTimeOffset().ShouldBe(At1400);
        upcoming.ShouldAllBe(r => r.GetProperty("teacherName").GetString() == teacherName && r.GetProperty("studentName").GetString() == "Ali Yılmaz");
        upcoming[3].GetProperty("requestedByMe").GetBoolean().ShouldBeFalse();
        upcoming[3].GetProperty("requestedByName").GetString().ShouldBe(fatherName);
        upcoming.ShouldAllBe(r => !r.GetProperty("canCancel").GetBoolean(), "annenin açık randevusu yok; babanınkini iptal edemez");
        JsonElement[] history = Rows(mine, "past");
        history.Select(r => r.GetProperty("id").GetGuid()).ShouldBe([expired, approvedPast], "en yeni üstte; 31 gün önceki görünmez");
        history.Select(r => r.GetProperty("state").GetString()).ShouldBe(["Expired", "Approved"]);
        mine.GetRawText().ShouldNotContain(old.ToString());

        JsonElement fatherView = (await MineAsync(client, father)).Body;
        Rows(fatherView, "upcoming").Single(r => r.GetProperty("id").GetGuid() == pending).GetProperty("canCancel").GetBoolean().ShouldBeTrue();
        Rows(fatherView, "upcoming").Length.ShouldBe(4);
        (await MineAsync(client, await ReloginAsync(client, w.Class, w.AyseMother))).Body.GetProperty("upcoming").GetArrayLength().ShouldBe(0, "başka aile");
    }

    private static async Task<(string Teacher, string Mother, string Father)> AdvanceAsync(FakeTimeProvider time, DateTimeOffset to, HttpClient client, CommunicationWorld w)
    {
        time.SetUtcNow(to);
        return (
            await TokenForSchoolAsync(client, w.Class.TeacherEmail, w.Class.School.TenantId),
            await ReloginAsync(client, w.Class, w.AliMother),
            await ReloginAsync(client, w.Class, w.AliFather));
    }
}
