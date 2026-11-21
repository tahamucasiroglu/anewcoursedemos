using System.Net;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Appointments.AppointmentsTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Appointments;

/// <summary>
/// Spec 0016 AC-2: öğretmenin başlamamış bir dilimiyle çakışan aralık "Bu saatlerde zaten müsaitliğiniz var." ile reddedilir ve hiçbir
/// şey oluşmaz; bitişik aralık kabul edilir; kaldırılmış dilimin saati yeniden açılabilir; boş dilim kaldırılır (takvimden ve velinin
/// listesinden düşer); onay bekleyen ya da onaylı dilim "Önce talebi yanıtlayın ya da randevuyu iptal edin." ile reddedilir; başka
/// öğretmenin dilimi 404.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AvailabilityOverlapAndRemoveTests
{
    private readonly TestDatabase database;

    public AvailabilityOverlapAndRemoveTests(TestDatabase database)
    {
        this.database = database;
    }

    [Theory]
    [InlineData("15:00", "17:00")]
    [InlineData("13:00", "14:30")]
    [InlineData("14:15", "14:45")]
    [InlineData("12:00", "18:00")]
    public async Task Open_OverlappingRange_RejectedNothingCreated(string start, string end)
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await OpenedAsync(client, w.Class.Teacher);

        var (status, body, _) = await OpenAsync(client, w.Class.Teacher, start: start, end: end, durationMinutes: 20);

        status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(body).ShouldBe("Bu saatlerde zaten müsaitliğiniz var.");
        (await SlotRowsAsync(factory, w.Class.School.TenantId)).Count.ShouldBe(6);
    }

    [Fact]
    public async Task Open_AdjacentRangesAndOtherTeacher_Accepted()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await OpenedAsync(client, w.Class.Teacher);
        string other = w.OtherTeacher;

        (await OpenedAsync(client, w.Class.Teacher, start: "16:00", end: "17:00", durationMinutes: 30)).ShouldBe(2);
        (await OpenedAsync(client, w.Class.Teacher, start: "13:00", end: "14:00", durationMinutes: 15)).ShouldBe(4);
        (await OpenedAsync(client, other, start: "14:00", end: "16:00", durationMinutes: 20)).ShouldBe(6, "çakışma öğretmen başınadır");
    }

    [Fact]
    public async Task Remove_FreeSlot_GoneAndHourReopenable()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await OpenedAsync(client, w.Class.Teacher, start: "14:00", end: "15:00", durationMinutes: 30);
        Guid[] free = await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali);

        (await RemoveSlotAsync(client, w.Class.Teacher, free[0])).Status.ShouldBe(HttpStatusCode.NoContent);

        StateOf((await ScheduleAsync(client, w.Class.Teacher)).Body, free[0]).ShouldBeNull();
        (await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali)).ShouldBe([free[1]]);
        (await RemoveSlotAsync(client, w.Class.Teacher, free[0])).Status.ShouldBe(HttpStatusCode.NotFound, "kaldırılmış dilim bulunamaz");
        (await OpenedAsync(client, w.Class.Teacher, start: "14:00", end: "14:30", durationMinutes: 30)).ShouldBe(1, "kaldırılan saat yeniden açılabilir");
    }

    [Fact]
    public async Task Remove_PendingOrApprovedSlot_Rejected()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await OpenedAsync(client, w.Class.Teacher, start: "14:00", end: "15:00", durationMinutes: 30);
        Guid[] free = await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali);
        await RequestedAsync(client, w.AliMother.Token, free[0], w.Ali);
        Guid approved = await RequestedAsync(client, w.AyseMother.Token, free[1], w.Ayse);
        (await RespondAsync(client, w.Class.Teacher, approved, "Approve")).Status.ShouldBe(HttpStatusCode.OK);

        foreach (Guid slot in free)
        {
            var (status, body, _) = await RemoveSlotAsync(client, w.Class.Teacher, slot);
            status.ShouldBe(HttpStatusCode.BadRequest);
            Detail(body).ShouldBe("Önce talebi yanıtlayın ya da randevuyu iptal edin.");
        }

        (await SlotRowsAsync(factory, w.Class.School.TenantId)).ShouldAllBe(s => s.RemovedAt == null);
    }

    [Fact]
    public async Task Remove_OtherTeachersSlot_NotFound()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        string other = w.OtherTeacher;
        await OpenedAsync(client, w.Class.Teacher, start: "14:00", end: "14:30", durationMinutes: 30);
        Guid slot = (await SlotRowsAsync(factory, w.Class.School.TenantId)).Single().Id;

        (await RemoveSlotAsync(client, other, slot)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await SlotRowsAsync(factory, w.Class.School.TenantId)).Single().RemovedAt.ShouldBeNull();
    }
}
