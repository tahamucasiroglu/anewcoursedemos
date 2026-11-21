using System.Net;
using System.Text.Json;
using OkulYonetim.Domain.Appointments;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Academics.AcademicsTestData;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Appointments.AppointmentsTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Appointments;

/// <summary>
/// Spec 0016 AC-11 (yetki): veli ilişkisi olmayan öğretmenin dilimlerini göremez ve talep edemez (404), bağlı olmadığı öğrenci adına talep
/// edemez (404), bağlı olmadığı çocuğun randevusunu göremez ve iptal edemez (404), çocuğun öteki velisinin talep ettiği randevuyu görür ama
/// iptal edemez (403); öğretmen rolü olmayan müsaitlik ve yanıt işlemlerinde, veli rolü olmayan talep işlemlerinde 403 (Türkçe gövde);
/// ilişki bitince (öğrenci sınıftan ayrıldı, öğretmen sınıftan alındı, veli bağı kalktı) yeni talep reddedilir, var olan randevu görünür ve
/// iptal edilebilir; girişsiz istek 401.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AppointmentAuthorizationTests
{
    private readonly TestDatabase database;

    public AppointmentAuthorizationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Guardian_UnrelatedTeacherOrChild_NotFound()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await OpenedAsync(client, w.Class.Teacher, start: "14:00", end: "14:30", durationMinutes: 30);
        await OpenedAsync(client, w.OtherTeacher, start: "14:00", end: "14:30", durationMinutes: 30);
        Guid classSlot = (await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali)).Single();
        Guid otherSlot = (await FreeSlotIdsAsync(client, w.BerkMother.Token, w.OtherTeacherId, w.Berk)).Single();
        Guid berks = await RequestedAsync(client, w.BerkMother.Token, otherSlot, w.Berk);

        (await SlotsAsync(client, w.AliMother.Token, w.OtherTeacherId, w.Ali)).Status.ShouldBe(HttpStatusCode.NotFound, "ilişkisiz öğretmen");
        (await RequestAsync(client, w.AliMother.Token, otherSlot, w.Ali)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await TeachersAsync(client, w.AliMother.Token, w.Berk)).Status.ShouldBe(HttpStatusCode.NotFound, "bağlı olmadığı çocuk");
        (await SlotsAsync(client, w.AliMother.Token, w.OtherTeacherId, w.Berk)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await RequestAsync(client, w.AliMother.Token, classSlot, w.Berk)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await CancelAsync(client, w.AliMother.Token, berks)).Status.ShouldBe(HttpStatusCode.NotFound, "başka ailenin randevusu");
        (await MineAsync(client, w.AliMother.Token)).Body.GetRawText().ShouldNotContain(berks.ToString());
        (await AppointmentRowsAsync(factory, w.Class.School.TenantId)).Single().Status.ShouldBe(AppointmentStatus.Pending);
    }

    [Fact]
    public async Task OtherParent_SeesButCannotCancel()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await OpenedAsync(client, w.Class.Teacher, start: "14:00", end: "14:30", durationMinutes: 30);
        Guid id = await RequestedAsync(client, w.AliMother.Token, (await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali)).Single(), w.Ali);

        JsonElement row = Rows((await MineAsync(client, w.AliFather.Token)).Body, "upcoming").Single();
        var (status, body, _) = await CancelAsync(client, w.AliFather.Token, id);

        row.GetProperty("id").GetGuid().ShouldBe(id);
        row.GetProperty("requestedByMe").GetBoolean().ShouldBeFalse();
        status.ShouldBe(HttpStatusCode.Forbidden);
        Detail(body).ShouldBe("Randevuyu yalnızca talep eden veli iptal edebilir.");
        (await AppointmentRowsAsync(factory, w.Class.School.TenantId)).Single().Status.ShouldBe(AppointmentStatus.Pending);
    }

    [Fact]
    public async Task WrongRole_Forbidden_WithTurkishBody()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await OpenedAsync(client, w.Class.Teacher, start: "14:00", end: "14:30", durationMinutes: 30);
        Guid slot = (await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali)).Single();
        Guid id = await RequestedAsync(client, w.AliMother.Token, slot, w.Ali);
        string guardian = w.AliMother.Token;

        foreach (var (status, body, _) in new[]
        {
            await ScheduleAsync(client, guardian),
            await OpenAsync(client, guardian),
            await RemoveSlotAsync(client, guardian, slot),
            await RespondAsync(client, guardian, id, "Approve"),
        })
        {
            status.ShouldBe(HttpStatusCode.Forbidden);
            Detail(body).ShouldBe("Bu işlem yalnızca öğretmenler içindir.");
        }

        foreach (var (status, body, _) in new[]
        {
            await TeachersAsync(client, w.Class.Teacher, w.Ali),
            await SlotsAsync(client, w.Class.Teacher, w.Class.TeacherId, w.Ali),
            await RequestAsync(client, w.Class.Teacher, slot, w.Ali),
            await MineAsync(client, w.Class.Teacher),
        })
        {
            status.ShouldBe(HttpStatusCode.Forbidden);
            Detail(body).ShouldBe("Bu işlem yalnızca veliler içindir.");
        }

        (await CancelAsync(client, w.Class.Founder, id)).Status.ShouldBe(HttpStatusCode.Forbidden, "kurucu ne öğretmen ne veli");
        (await ScheduleAsync(client, w.Class.Founder)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await CancelAsync(client, w.OtherTeacher, id)).Status.ShouldBe(HttpStatusCode.NotFound, "başka öğretmenin randevusu");
        (await AppointmentRowsAsync(factory, w.Class.School.TenantId)).Single().Status.ShouldBe(AppointmentStatus.Pending);
    }

    [Fact]
    public async Task Anonymous_Unauthorized()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();

        foreach ((HttpMethod method, string path) in new[]
        {
            (HttpMethod.Get, "/api/appointments/schedule"),
            (HttpMethod.Post, "/api/appointments/availability"),
            (HttpMethod.Get, "/api/appointments/mine"),
            (HttpMethod.Post, "/api/appointments"),
            (HttpMethod.Post, $"/api/appointments/{Guid.NewGuid()}/cancel"),
        })
        {
            using HttpRequestMessage request = new(method, path);
            if (method == HttpMethod.Post)
            {
                request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
            }

            using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, path);
        }
    }

    [Fact]
    public async Task StudentLeavesClass_NewRequestRejected_ExistingVisibleAndCancellable()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid id, Guid spare) = await ExistingAsync(client, w);

        await MoveAsync(client, w.Class.Founder, w.Ali, "Ali Yılmaz", w.Class.School.BranchId, null);

        await AssertEndedAsync(client, w, id, spare);
    }

    [Fact]
    public async Task TeacherRemovedFromClass_NewRequestRejected_ExistingVisibleAndCancellable()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid id, Guid spare) = await ExistingAsync(client, w);

        await AssignAsync(client, w.Class.Founder, w.Class.ClassroomId);

        await AssertEndedAsync(client, w, id, spare);
    }

    [Fact]
    public async Task GuardianLinkRemoved_NewRequestRejected_OwnAppointmentVisibleAndCancellable()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid id, Guid spare) = await ExistingAsync(client, w);

        (await StatusAsync(client, HttpMethod.Delete, $"/api/admin/students/{w.Ali}/guardians/{w.AliMother.UserId}", w.Class.Founder)).ShouldBe(HttpStatusCode.OK);

        await AssertEndedAsync(client, w, id, spare);
    }

    /// <summary>Annenin Ali için onay bekleyen randevusu ve aynı öğretmenin boş bir dilimi.</summary>
    private static async Task<(Guid Id, Guid Spare)> ExistingAsync(HttpClient client, CommunicationWorld w)
    {
        await OpenedAsync(client, w.Class.Teacher, start: "14:00", end: "15:00", durationMinutes: 30);
        Guid[] free = await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali);
        return (await RequestedAsync(client, w.AliMother.Token, free[0], w.Ali), free[1]);
    }

    private static async Task AssertEndedAsync(HttpClient client, CommunicationWorld w, Guid id, Guid spare)
    {
        Rows((await MineAsync(client, w.AliMother.Token)).Body, "upcoming").Single().GetProperty("state").GetString().ShouldBe("Pending", "var olan randevu görünür");
        (await ScheduleAsync(client, w.Class.Teacher)).Body.GetProperty("pending").EnumerateArray().Single().GetProperty("appointmentId").GetGuid().ShouldBe(id);
        (await RequestAsync(client, w.AliMother.Token, spare, w.Ali)).Status.ShouldBe(HttpStatusCode.NotFound, "ilişki bitti: yeni talep yok");
        (await CancelAsync(client, w.AliMother.Token, id)).Status.ShouldBe(HttpStatusCode.OK, "var olan randevu iptal edilebilir");
        (await SlotsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali)).Status.ShouldBe(HttpStatusCode.NotFound);
        JsonElement row = Rows((await MineAsync(client, w.AliMother.Token)).Body, "upcoming").Single();
        row.GetProperty("id").GetGuid().ShouldBe(id);
        row.GetProperty("state").GetString().ShouldBe("CancelledByGuardian");
    }
}
