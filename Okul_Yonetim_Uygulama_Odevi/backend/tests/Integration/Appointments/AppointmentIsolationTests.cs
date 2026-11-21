using System.Net;
using OkulYonetim.Domain.Appointments;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Appointments.AppointmentsTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Appointments;

/// <summary>
/// Spec 0016 AC-11 (okul izolasyonu, S1): başka okulun dilimi ve randevusu 404 — öğretmen kaldıramaz, yanıtlayamaz, iptal edemez; veli
/// talep edemez, boş saatlerini göremez, iptal edemez; takvim ve "Randevularım" başka okulun kaydını içermez; başka okulun kaydı değişmez.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AppointmentIsolationTests
{
    private readonly TestDatabase database;

    public AppointmentIsolationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task OtherSchoolsSlotsAndAppointments_NotFound()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld a = await WorldAsync(factory, client);
        CommunicationWorld b = await WorldAsync(factory, client);
        await OpenedAsync(client, b.Class.Teacher, start: "14:00", end: "15:00", durationMinutes: 30);
        Guid[] bSlots = await FreeSlotIdsAsync(client, b.AliMother.Token, b.Class.TeacherId, b.Ali);
        Guid bAppointment = await RequestedAsync(client, b.AliMother.Token, bSlots[0], b.Ali);

        (await RemoveSlotAsync(client, a.Class.Teacher, bSlots[1])).Status.ShouldBe(HttpStatusCode.NotFound);
        (await RespondAsync(client, a.Class.Teacher, bAppointment, "Approve")).Status.ShouldBe(HttpStatusCode.NotFound);
        (await CancelAsync(client, a.Class.Teacher, bAppointment)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await RequestAsync(client, a.AliMother.Token, bSlots[1], a.Ali)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await SlotsAsync(client, a.AliMother.Token, b.Class.TeacherId, a.Ali)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await TeachersAsync(client, a.AliMother.Token, b.Ali)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await CancelAsync(client, a.AliMother.Token, bAppointment)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await ScheduleAsync(client, a.Class.Teacher)).Body.GetProperty("slots").GetArrayLength().ShouldBe(0);
        (await MineAsync(client, a.AliMother.Token)).Body.GetProperty("upcoming").GetArrayLength().ShouldBe(0);
        (await MeAsync(client, a.Class.Teacher)).GetProperty("pendingAppointmentCount").GetInt32().ShouldBe(0);

        (await AppointmentRowsAsync(factory, b.Class.School.TenantId)).Single().Status.ShouldBe(AppointmentStatus.Pending);
        (await SlotRowsAsync(factory, b.Class.School.TenantId)).ShouldAllBe(s => s.RemovedAt == null);
        (await AppointmentRowsAsync(factory, a.Class.School.TenantId)).ShouldBeEmpty();
    }
}
