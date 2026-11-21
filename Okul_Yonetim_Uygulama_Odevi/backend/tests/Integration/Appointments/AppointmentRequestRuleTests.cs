using System.Net;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Domain.Appointments;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Appointments.AppointmentsTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;
using static OkulYonetim.Tests.Integration.Students.StudentsTestData;

namespace OkulYonetim.Tests.Integration.Appointments;

/// <summary>
/// Spec 0016 AC-5: başlamış dilime talep ("Bu saat geçti; başka bir saat seçin."), boş olmayan dilime talep ("Bu saat artık dolu."),
/// aynı çocuk ve öğretmen için açık randevu varken — öteki veli istemiş olsa bile — ikinci talep ("Bu öğretmenle bu çocuk için zaten açık
/// bir randevunuz var.") ve boş ya da 301 karakterlik konu reddedilir, hiçbir şey değişmez (300 karakter kabul); aynı veli başka çocuğu
/// ya da başka öğretmen için talep edebilir; ret sonrası aynı çocuk için yeniden talep edilebilir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AppointmentRequestRuleTests
{
    private readonly TestDatabase database;

    public AppointmentRequestRuleTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Request_StartedSlot_RejectedNothingChanges()
    {
        FakeTimeProvider time = new(Morning);
        using OkulApiFactory factory = AppointmentsFactory(this.database, time: time);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await OpenedAsync(client, w.Class.Teacher, day: "2026-11-20", start: "12:30", end: "13:00", durationMinutes: 30);
        Guid slot = (await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali)).Single();

        time.Advance(TimeSpan.FromMinutes(30));
        string mother = await ReloginAsync(client, w.Class, w.AliMother);
        var (status, body, _) = await RequestAsync(client, mother, slot, w.Ali);

        status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(body).ShouldBe("Bu saat geçti; başka bir saat seçin.");
        (await AppointmentRowsAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty();
        (await SlotsAsync(client, mother, w.Class.TeacherId, w.Ali)).Body.GetArrayLength().ShouldBe(0, "başlamış dilim listede yok");
    }

    [Fact]
    public async Task Request_TakenSlot_RejectedNothingChanges()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await OpenedAsync(client, w.Class.Teacher, start: "14:00", end: "14:30", durationMinutes: 30);
        Guid slot = (await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali)).Single();
        await RequestedAsync(client, w.AliMother.Token, slot, w.Ali);

        var (status, body, _) = await RequestAsync(client, w.AyseMother.Token, slot, w.Ayse);

        status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(body).ShouldBe("Bu saat artık dolu.");
        (await AppointmentRowsAsync(factory, w.Class.School.TenantId)).Single().GuardianUserId.ShouldBe(w.AliMother.UserId);
    }

    [Fact]
    public async Task Request_OpenAppointmentForChildAndTeacher_RejectedEvenFromOtherParent()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await OpenedAsync(client, w.Class.Teacher, start: "14:00", end: "15:00", durationMinutes: 30);
        Guid[] free = await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali);
        Guid first = await RequestedAsync(client, w.AliMother.Token, free[0], w.Ali);

        var (sameParent, sameBody, _) = await RequestAsync(client, w.AliMother.Token, free[1], w.Ali);
        var (otherParent, otherBody, _) = await RequestAsync(client, w.AliFather.Token, free[1], w.Ali);
        (await RespondAsync(client, w.Class.Teacher, first, "Approve")).Status.ShouldBe(HttpStatusCode.OK);
        var (afterApprove, approveBody, _) = await RequestAsync(client, w.AliFather.Token, free[1], w.Ali);

        foreach ((HttpStatusCode status, System.Text.Json.JsonElement body) in new[] { (sameParent, sameBody), (otherParent, otherBody), (afterApprove, approveBody) })
        {
            status.ShouldBe(HttpStatusCode.BadRequest);
            Detail(body).ShouldBe("Bu öğretmenle bu çocuk için zaten açık bir randevunuz var.");
        }

        (await AppointmentRowsAsync(factory, w.Class.School.TenantId)).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Request_AfterRejection_SameChildMayRequestAgain()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await OpenedAsync(client, w.Class.Teacher, start: "14:00", end: "14:30", durationMinutes: 30);
        Guid slot = (await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali)).Single();
        Guid first = await RequestedAsync(client, w.AliMother.Token, slot, w.Ali);
        (await RespondAsync(client, w.Class.Teacher, first, "Reject")).Status.ShouldBe(HttpStatusCode.OK);

        (await RequestAsync(client, w.AliMother.Token, slot, w.Ali)).Status.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(null, "Konu zorunludur")]
    [InlineData("   ", "Konu zorunludur")]
    [InlineData(301, "Konu en fazla 300 karakter olabilir")]
    public async Task Request_InvalidTopic_TurkishFieldErrorNothingChanges(object? topic, string message)
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await OpenedAsync(client, w.Class.Teacher, start: "14:00", end: "14:30", durationMinutes: 30);
        Guid slot = (await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali)).Single();
        string? text = topic is int length ? new string('k', length) : (string?)topic;

        var (status, body, _) = await RequestAsync(client, w.AliMother.Token, slot, w.Ali, text);

        status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(body).ShouldBe(message);
        Messages(body).ShouldContain(message);
        (await AppointmentRowsAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty();
        (await RequestAsync(client, w.AliMother.Token, slot, w.Ali, new string('k', 300))).Status.ShouldBe(HttpStatusCode.OK, "300 karakter kabul");
    }

    [Fact]
    public async Task Request_SameGuardianOtherChildOrOtherTeacher_Allowed()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await LinkAsync(client, w.Class.Founder, w.Ayse, Guardian(w.AliMother.Email));
        await LinkAsync(client, w.Class.Founder, w.Berk, Guardian(w.AliMother.Email));
        await OpenedAsync(client, w.Class.Teacher, start: "14:00", end: "15:00", durationMinutes: 30);
        await OpenedAsync(client, w.OtherTeacher, start: "14:00", end: "14:30", durationMinutes: 30);
        Guid[] mine = await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali);
        Guid other = (await FreeSlotIdsAsync(client, w.AliMother.Token, w.OtherTeacherId, w.Berk)).Single();

        await RequestedAsync(client, w.AliMother.Token, mine[0], w.Ali);
        (await RequestAsync(client, w.AliMother.Token, mine[1], w.Ayse)).Status.ShouldBe(HttpStatusCode.OK, "başka çocuk");
        (await RequestAsync(client, w.AliMother.Token, other, w.Berk)).Status.ShouldBe(HttpStatusCode.OK, "başka öğretmen");

        (await AppointmentRowsAsync(factory, w.Class.School.TenantId)).Count.ShouldBe(3);
    }
}
