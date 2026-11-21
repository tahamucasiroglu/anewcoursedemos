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
/// Spec 0016 AC-7: dilimin öğretmeni onay bekleyen talebi onaylar ya da nedenli/nedensiz reddeder; ret dilimi yeniden boşaltır ve başka
/// veli talep edebilir; başka öğretmen yanıtlamaya çalışırsa 404; yanıtlanmış talep ("Bu talep zaten yanıtlandı.") ya da başlamış talep
/// ("Bu talebin süresi doldu.") yanıtlanamaz; dilim başlayınca yanıtlanmamış talep veliye ve öğretmene "Süresi doldu" görünür, bekleyen
/// sayısından düşer; geçersiz yanıt ve 301 karakterlik neden Türkçe alan hatası.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AppointmentRespondTests
{
    private readonly TestDatabase database;

    public AppointmentRespondTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Approve_PendingBecomesApproved()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid id, Guid slot) = await PendingAsync(client, w);

        var (status, body, _) = await RespondAsync(client, w.Class.Teacher, id, "Approve");

        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("state").GetString().ShouldBe("Approved");
        StateOf((await ScheduleAsync(client, w.Class.Teacher)).Body, slot).ShouldBe("Approved");
        Rows((await MineAsync(client, w.AliMother.Token)).Body, "upcoming").Single().GetProperty("state").GetString().ShouldBe("Approved");
        (await FreeSlotIdsAsync(client, w.AyseMother.Token, w.Class.TeacherId, w.Ayse)).ShouldNotContain(slot);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("O gün okulda değilim")]
    public async Task Reject_FreesSlotForOthers(string? reason)
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid id, Guid slot) = await PendingAsync(client, w);

        var (status, body, _) = await RespondAsync(client, w.Class.Teacher, id, "Reject", reason);

        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("state").GetString().ShouldBe("Rejected");
        Appointment saved = (await AppointmentRowsAsync(factory, w.Class.School.TenantId)).Single();
        saved.Status.ShouldBe(AppointmentStatus.Rejected);
        saved.Reason.ShouldBe(reason);
        StateOf((await ScheduleAsync(client, w.Class.Teacher)).Body, slot).ShouldBe("Free");
        JsonElement row = Rows((await MineAsync(client, w.AliMother.Token)).Body, "upcoming").Single();
        row.GetProperty("state").GetString().ShouldBe("Rejected");
        (reason is null ? row.GetProperty("reason").ValueKind == JsonValueKind.Null : row.GetProperty("reason").GetString() == reason).ShouldBeTrue();
        (await FreeSlotIdsAsync(client, w.AyseMother.Token, w.Class.TeacherId, w.Ayse)).ShouldContain(slot);
        (await RequestAsync(client, w.AyseMother.Token, slot, w.Ayse)).Status.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Respond_OtherTeacher_NotFound()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid id, _) = await PendingAsync(client, w);

        (await RespondAsync(client, w.OtherTeacher, id, "Approve")).Status.ShouldBe(HttpStatusCode.NotFound);
        (await RespondAsync(client, w.OtherTeacher, id, "Reject")).Status.ShouldBe(HttpStatusCode.NotFound);
        (await AppointmentRowsAsync(factory, w.Class.School.TenantId)).Single().Status.ShouldBe(AppointmentStatus.Pending);
    }

    [Theory]
    [InlineData("Approve", "Approve")]
    [InlineData("Approve", "Reject")]
    [InlineData("Reject", "Approve")]
    public async Task Respond_AlreadyAnswered_Rejected(string first, string second)
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid id, _) = await PendingAsync(client, w);
        (await RespondAsync(client, w.Class.Teacher, id, first)).Status.ShouldBe(HttpStatusCode.OK);

        var (status, body, _) = await RespondAsync(client, w.Class.Teacher, id, second);

        status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(body).ShouldBe("Bu talep zaten yanıtlandı.");
    }

    [Fact]
    public async Task Started_PendingCannotBeAnswered_ShowsExpiredToBoth()
    {
        FakeTimeProvider time = new(Morning);
        using OkulApiFactory factory = AppointmentsFactory(this.database, time: time);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await OpenedAsync(client, w.Class.Teacher, day: "2026-11-20", start: "12:30", end: "13:00", durationMinutes: 30);
        Guid slot = (await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali)).Single();
        Guid id = await RequestedAsync(client, w.AliMother.Token, slot, w.Ali);
        (await MeAsync(client, w.Class.Teacher)).GetProperty("pendingAppointmentCount").GetInt32().ShouldBe(1);

        time.Advance(TimeSpan.FromMinutes(30));
        string teacher = await TokenForSchoolAsync(client, w.Class.TeacherEmail, w.Class.School.TenantId);
        string mother = await ReloginAsync(client, w.Class, w.AliMother);
        var (status, body, _) = await RespondAsync(client, teacher, id, "Approve");

        status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(body).ShouldBe("Bu talebin süresi doldu.");
        JsonElement schedule = (await ScheduleAsync(client, teacher)).Body;
        StateOf(schedule, slot).ShouldBe("Expired");
        schedule.GetProperty("pending").GetArrayLength().ShouldBe(0);
        schedule.GetProperty("pendingCount").GetInt32().ShouldBe(0);
        (await MeAsync(client, teacher)).GetProperty("pendingAppointmentCount").GetInt32().ShouldBe(0);
        Rows((await MineAsync(client, mother)).Body, "past").Single().GetProperty("state").GetString().ShouldBe("Expired");
        (await AppointmentRowsAsync(factory, w.Class.School.TenantId)).Single().Status.ShouldBe(AppointmentStatus.Pending, "süresi doldu saklanmaz");
    }

    [Fact]
    public async Task Respond_InvalidDecisionOrLongReason_FieldErrors()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid id, _) = await PendingAsync(client, w);

        var (s1, b1, _) = await RespondAsync(client, w.Class.Teacher, id, "Maybe");
        var (s2, b2, _) = await RespondAsync(client, w.Class.Teacher, id, "Reject", new string('n', 301));

        s1.ShouldBe(HttpStatusCode.BadRequest);
        Detail(b1).ShouldBe("Yanıt Onayla ya da Reddet olmalı");
        s2.ShouldBe(HttpStatusCode.BadRequest);
        Detail(b2).ShouldBe("Neden en fazla 300 karakter olabilir");
        (await AppointmentRowsAsync(factory, w.Class.School.TenantId)).Single().Status.ShouldBe(AppointmentStatus.Pending);
        (await RespondAsync(client, w.Class.Teacher, id, "Reject", new string('n', 300))).Status.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task<(Guid Id, Guid Slot)> PendingAsync(HttpClient client, CommunicationWorld w)
    {
        await OpenedAsync(client, w.Class.Teacher, start: "14:00", end: "15:00", durationMinutes: 30);
        Guid slot = (await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali))[0];
        return (await RequestedAsync(client, w.AliMother.Token, slot, w.Ali), slot);
    }
}
