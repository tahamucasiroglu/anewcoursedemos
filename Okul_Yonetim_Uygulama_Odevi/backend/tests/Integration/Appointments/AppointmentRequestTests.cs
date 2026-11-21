using System.Net;
using System.Text.Json;
using OkulYonetim.Domain.Appointments;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Appointments.AppointmentsTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Appointments;

/// <summary>
/// Spec 0016 AC-4: veli yalnızca çocuğunun sınıfının öğretmenlerini (başka sınıfın öğretmeni değil) ve onların başlamamış boş
/// dilimlerini görür; dilim seçip konuyla talep gönderince randevu "Onay bekliyor" olur ve dilim öteki velilerin boş listesinden düşer;
/// öğretmenin boş dilimi yoksa boş liste ("Bu öğretmenin boş görüşme saati yok" metni istemcide).
/// </summary>
[Trait("Category", "Integration")]
public sealed class AppointmentRequestTests
{
    private readonly TestDatabase database;

    public AppointmentRequestTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Teachers_OnlyChildsClassroomTeachers()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        JsonElement ali = (await TeachersAsync(client, w.AliMother.Token, w.Ali)).Body;
        JsonElement berk = (await TeachersAsync(client, w.BerkMother.Token, w.Berk)).Body;

        ali.EnumerateArray().Select(t => t.GetProperty("userId").GetGuid()).ShouldBe([w.Class.TeacherId]);
        ali[0].GetProperty("fullName").GetString().ShouldNotBeNullOrWhiteSpace();
        berk.EnumerateArray().Select(t => t.GetProperty("userId").GetGuid()).ShouldBe([w.OtherTeacherId]);
    }

    [Fact]
    public async Task Request_PendingAndSlotLeavesOtherGuardiansList()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await OpenedAsync(client, w.Class.Teacher, start: "14:00", end: "15:00", durationMinutes: 20);
        JsonElement[] listed = [.. (await SlotsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali)).Body.EnumerateArray()];
        listed.Length.ShouldBe(3);
        listed[0].GetProperty("startsAt").GetDateTimeOffset().ShouldBe(At1400);
        Guid slot = listed[1].GetProperty("slotId").GetGuid();

        var (status, body, _) = await RequestAsync(client, w.AliMother.Token, slot, w.Ali, "  Ali'nin matematik ödevleri  ");

        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("state").GetString().ShouldBe("Pending");
        Appointment saved = (await AppointmentRowsAsync(factory, w.Class.School.TenantId)).Single();
        saved.Id.ShouldBe(body.GetProperty("appointmentId").GetGuid());
        saved.Status.ShouldBe(AppointmentStatus.Pending);
        saved.SlotId.ShouldBe(slot);
        saved.TeacherUserId.ShouldBe(w.Class.TeacherId);
        saved.GuardianUserId.ShouldBe(w.AliMother.UserId);
        saved.StudentId.ShouldBe(w.Ali);
        saved.Topic.ShouldBe("Ali'nin matematik ödevleri");
        saved.StartsAt.ShouldBe(At1400.AddMinutes(20));
        (await FreeSlotIdsAsync(client, w.AyseMother.Token, w.Class.TeacherId, w.Ayse)).ShouldNotContain(slot);
        (await FreeSlotIdsAsync(client, w.AyseMother.Token, w.Class.TeacherId, w.Ayse)).Length.ShouldBe(2);
        Rows((await MineAsync(client, w.AliMother.Token)).Body, "upcoming").Single().GetProperty("state").GetString().ShouldBe("Pending");
    }

    [Fact]
    public async Task Slots_NoFreeSlot_EmptyList()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        var (status, body, _) = await SlotsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali);
        status.ShouldBe(HttpStatusCode.OK);
        body.GetArrayLength().ShouldBe(0);

        await OpenedAsync(client, w.Class.Teacher, start: "14:00", end: "14:30", durationMinutes: 30);
        Guid only = (await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali)).Single();
        await RequestedAsync(client, w.AliMother.Token, only, w.Ali);
        (await SlotsAsync(client, w.AyseMother.Token, w.Class.TeacherId, w.Ayse)).Body.GetArrayLength().ShouldBe(0);
    }
}
