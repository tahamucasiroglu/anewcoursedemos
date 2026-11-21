using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Domain.Appointments;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Appointments.AppointmentsTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Appointments;

/// <summary>
/// Spec 0016 AC-8: veli onay bekleyen ya da onaylı randevusunu başlamadan iptal eder ve dilim yeniden boşalır (başka veli alabilir);
/// öğretmen onaylı randevuyu başlamadan (isteğe bağlı nedenle) iptal eder ve dilim takvimden ve velilerin listesinden kalkar; başlamış
/// randevunun iptali "Başlamış randevu iptal edilemez." ile reddedilir (veli ve öğretmen); öğretmen onay bekleyen talebi iptal etmez,
/// reddeder; iptal edilmiş randevu yeniden iptal edilemez.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AppointmentCancelTests
{
    private readonly TestDatabase database;

    public AppointmentCancelTests(TestDatabase database)
    {
        this.database = database;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Guardian_CancelsPendingOrApproved_SlotFreedAgain(bool approveFirst)
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid id, Guid slot) = await RequestedSlotAsync(client, w);
        if (approveFirst)
        {
            (await RespondAsync(client, w.Class.Teacher, id, "Approve")).Status.ShouldBe(HttpStatusCode.OK);
        }

        var (status, body, _) = await CancelAsync(client, w.AliMother.Token, id);

        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("state").GetString().ShouldBe("CancelledByGuardian");
        StateOf((await ScheduleAsync(client, w.Class.Teacher)).Body, slot).ShouldBe("Free");
        Rows((await MineAsync(client, w.AliMother.Token)).Body, "upcoming").Single().GetProperty("state").GetString().ShouldBe("CancelledByGuardian");
        (await RequestAsync(client, w.AyseMother.Token, slot, w.Ayse)).Status.ShouldBe(HttpStatusCode.OK, "dilim başkasına açıldı");
        var (again, againBody, _) = await CancelAsync(client, w.AliMother.Token, id);
        again.ShouldBe(HttpStatusCode.BadRequest);
        Detail(againBody).ShouldBe("Bu randevu iptal edilemez.");
    }

    [Fact]
    public async Task Teacher_CancelsApproved_SlotLeavesSchedule()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid id, Guid slot) = await RequestedSlotAsync(client, w);
        (await RespondAsync(client, w.Class.Teacher, id, "Approve")).Status.ShouldBe(HttpStatusCode.OK);

        var (status, body, _) = await CancelAsync(client, w.Class.Teacher, id, "Hastayım");

        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("state").GetString().ShouldBe("CancelledByTeacher");
        StateOf((await ScheduleAsync(client, w.Class.Teacher)).Body, slot).ShouldBeNull();
        (await FreeSlotIdsAsync(client, w.AyseMother.Token, w.Class.TeacherId, w.Ayse)).ShouldNotContain(slot);
        (await SlotRowsAsync(factory, w.Class.School.TenantId)).Single(s => s.Id == slot).RemovedAt.ShouldNotBeNull();
        JsonElement row = Rows((await MineAsync(client, w.AliMother.Token)).Body, "upcoming").Single();
        row.GetProperty("state").GetString().ShouldBe("CancelledByTeacher");
        row.GetProperty("reason").GetString().ShouldBe("Hastayım");
        row.GetProperty("startsAt").GetDateTimeOffset().ShouldBe(At1400, "kapanan dilimin randevusu gün ve saatiyle görünür");
    }

    [Fact]
    public async Task Teacher_PendingRequest_MustRejectNotCancel()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid id, _) = await RequestedSlotAsync(client, w);

        var (status, body, _) = await CancelAsync(client, w.Class.Teacher, id);

        status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(body).ShouldBe("Onay bekleyen talebi iptal etmek yerine reddedin.");
        (await AppointmentRowsAsync(factory, w.Class.School.TenantId)).Single().Status.ShouldBe(AppointmentStatus.Pending);
    }

    [Fact]
    public async Task Started_CannotBeCancelledByEitherSide()
    {
        FakeTimeProvider time = new(Morning);
        using OkulApiFactory factory = AppointmentsFactory(this.database, time: time);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await OpenedAsync(client, w.Class.Teacher, day: "2026-11-20", start: "12:30", end: "13:00", durationMinutes: 30);
        Guid slot = (await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali)).Single();
        Guid id = await RequestedAsync(client, w.AliMother.Token, slot, w.Ali);
        (await RespondAsync(client, w.Class.Teacher, id, "Approve")).Status.ShouldBe(HttpStatusCode.OK);

        time.Advance(TimeSpan.FromMinutes(30));
        string teacher = await TokenForSchoolAsync(client, w.Class.TeacherEmail, w.Class.School.TenantId);
        string mother = await ReloginAsync(client, w.Class, w.AliMother);

        foreach (string token in new[] { mother, teacher })
        {
            var (status, body, _) = await CancelAsync(client, token, id);
            status.ShouldBe(HttpStatusCode.BadRequest);
            Detail(body).ShouldBe("Başlamış randevu iptal edilemez.");
        }

        (await AppointmentRowsAsync(factory, w.Class.School.TenantId)).Single().Status.ShouldBe(AppointmentStatus.Approved);
        Rows((await MineAsync(client, mother)).Body, "past").Single().GetProperty("canCancel").GetBoolean().ShouldBeFalse();
    }

    private static async Task<(Guid Id, Guid Slot)> RequestedSlotAsync(HttpClient client, CommunicationWorld w)
    {
        await OpenedAsync(client, w.Class.Teacher, start: "14:00", end: "14:30", durationMinutes: 30);
        Guid slot = (await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali)).Single();
        return (await RequestedAsync(client, w.AliMother.Token, slot, w.Ali), slot);
    }
}
