using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Appointments.AppointmentsTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Appointments;

/// <summary>
/// Spec 0016 AC-3: öğretmen bugünden itibaren dilimlerini gün ve saat sırasıyla, durumlarıyla görür (boş / onay bekliyor + veli, çocuk,
/// konu / onaylı); onay bekleyenler ayrı üst listede; hoş geldin'in kaynağı <c>/api/me</c> <c>pendingAppointmentCount</c> doğru (veli ve
/// başka öğretmen için 0); dilim yoksa boş liste ("Henüz müsaitlik açmadınız" metni istemcide).
/// </summary>
[Trait("Category", "Integration")]
public sealed class TeacherScheduleTests
{
    private readonly TestDatabase database;

    public TeacherScheduleTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Schedule_NoSlots_EmptyAndZeroPending()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        var (status, body, _) = await ScheduleAsync(client, w.Class.Teacher);

        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("slots").GetArrayLength().ShouldBe(0);
        body.GetProperty("pending").GetArrayLength().ShouldBe(0);
        body.GetProperty("pendingCount").GetInt32().ShouldBe(0);
        (await MeAsync(client, w.Class.Teacher)).GetProperty("pendingAppointmentCount").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task Schedule_OrderedByDayAndTime_StatesAndPendingOnTop()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await OpenedAsync(client, w.Class.Teacher, day: "2026-11-26", start: "14:00", end: "15:00", durationMinutes: 30);
        await OpenedAsync(client, w.Class.Teacher, day: "2026-11-25", start: "10:00", end: "11:00", durationMinutes: 30);
        Guid[] free = await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali);
        Guid pending = await RequestedAsync(client, w.AliMother.Token, free[2], w.Ali, "Ali'nin okuma alışkanlığı");
        Guid approved = await RequestedAsync(client, w.AyseMother.Token, free[0], w.Ayse, "Ayşe'nin devamsızlığı");
        (await RespondAsync(client, w.Class.Teacher, approved, "Approve")).Status.ShouldBe(HttpStatusCode.OK);

        JsonElement body = (await ScheduleAsync(client, w.Class.Teacher)).Body;

        JsonElement[] slots = [.. body.GetProperty("slots").EnumerateArray()];
        slots.Select(s => s.GetProperty("slotId").GetGuid()).ShouldBe(free);
        slots.Select(s => s.GetProperty("startsAt").GetDateTimeOffset()).ShouldBeInOrder();
        slots.Select(s => s.GetProperty("state").GetString()).ShouldBe(["Approved", "Free", "Pending", "Free"]);
        slots[0].GetProperty("startsAt").GetDateTimeOffset().ShouldBe(new DateTimeOffset(2026, 11, 25, 7, 0, 0, TimeSpan.Zero));
        JsonElement top = body.GetProperty("pending").EnumerateArray().Single();
        top.GetProperty("appointmentId").GetGuid().ShouldBe(pending);
        top.GetProperty("studentName").GetString().ShouldBe("Ali Yılmaz");
        top.GetProperty("guardianName").GetString().ShouldNotBeNullOrWhiteSpace();
        top.GetProperty("topic").GetString().ShouldBe("Ali'nin okuma alışkanlığı");
        slots[0].GetProperty("studentName").GetString().ShouldBe("Ayşe Kaya");
        slots[1].GetProperty("appointmentId").ValueKind.ShouldBe(JsonValueKind.Null);
        body.GetProperty("pendingCount").GetInt32().ShouldBe(1);
        (await MeAsync(client, w.Class.Teacher)).GetProperty("pendingAppointmentCount").GetInt32().ShouldBe(1);
        (await MeAsync(client, w.OtherTeacher)).GetProperty("pendingAppointmentCount").GetInt32().ShouldBe(0);
        (await MeAsync(client, w.AliMother.Token)).GetProperty("pendingAppointmentCount").GetInt32().ShouldBe(0);
    }
}
