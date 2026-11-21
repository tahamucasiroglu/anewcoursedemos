using System.Net;
using OkulYonetim.Domain.Appointments;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Appointments.AppointmentsTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Appointments;

/// <summary>
/// Spec 0016 AC-11 (okul izolasyonu, S1): başka okulun dilimi ve randevusu 404 — öğretmen kaldıramaz, yanıtlayamaz, iptal edemez; veli
/// talep edemez, boş saatlerini göremez, iptal edemez; takvim ve "Randevularım" başka okulun kaydını içermez; başka okulun kaydı değişmez.
/// İki okulda öğretmen olan kişinin takvimi okul başınadır (kurum filtresi): bir okulun oturumunda öteki okulun dilimi ve talebi görünmez,
/// aynı saatler çakışmasız açılır.
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

    [Fact]
    public async Task TeacherInTwoSchools_CalendarsAreSeparatePerSchool()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld a = await WorldAsync(factory, client);
        CommunicationWorld b = await WorldAsync(factory, client);
        await AddMemberAsync(factory, a.Class.TeacherEmail, b.Class.School.TenantId, Role.Teacher, b.Class.School.BranchId);
        await AssignAsync(client, b.Class.Founder, b.Class.ClassroomId, b.Class.TeacherId, a.Class.TeacherId);
        string inB = await TokenForSchoolAsync(client, a.Class.TeacherEmail, b.Class.School.TenantId);
        await OpenedAsync(client, inB, start: "14:00", end: "15:00", durationMinutes: 30);
        Guid bSlot = (await FreeSlotIdsAsync(client, b.AliMother.Token, a.Class.TeacherId, b.Ali))[0];
        Guid bAppointment = await RequestedAsync(client, b.AliMother.Token, bSlot, b.Ali);

        // Okul A oturumunda: B'nin dilimi ve talebi görünmez; aynı saatler A'da çakışmasız açılır (takvim okul başına).
        var (status, schedule, _) = await ScheduleAsync(client, a.Class.Teacher);
        status.ShouldBe(HttpStatusCode.OK);
        schedule.GetProperty("slots").GetArrayLength().ShouldBe(0);
        (await MeAsync(client, a.Class.Teacher)).GetProperty("pendingAppointmentCount").GetInt32().ShouldBe(0);
        (await RespondAsync(client, a.Class.Teacher, bAppointment, "Approve")).Status.ShouldBe(HttpStatusCode.NotFound);
        (await RemoveSlotAsync(client, a.Class.Teacher, (await FreeSlotIdsAsync(client, b.AliMother.Token, a.Class.TeacherId, b.Ali))[0])).Status.ShouldBe(HttpStatusCode.NotFound);
        (await OpenedAsync(client, a.Class.Teacher, start: "14:00", end: "15:00", durationMinutes: 30)).ShouldBe(2);
        (await FreeSlotIdsAsync(client, a.AliMother.Token, a.Class.TeacherId, a.Ali)).Length.ShouldBe(2, "A velisi yalnızca A'nın dilimlerini görür");
        (await ScheduleAsync(client, inB)).Body.GetProperty("pendingCount").GetInt32().ShouldBe(1);
        (await AppointmentRowsAsync(factory, b.Class.School.TenantId)).Single().Status.ShouldBe(AppointmentStatus.Pending);
    }
}
