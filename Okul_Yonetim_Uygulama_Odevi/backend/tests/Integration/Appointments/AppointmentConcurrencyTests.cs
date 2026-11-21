using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Appointments;
using OkulYonetim.Persistence.Repositories;
using OkulYonetim.Tests.Integration.Administration;
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
/// Spec 0016 AC-6 (eşzamanlılık, belirlenimci — <see cref="RaceGate"/>): iki veli aynı dilime aynı anda talep gönderirse yalnızca biri
/// "Onay bekliyor" olur, öteki "Bu saat artık dolu." alır; aynı velinin aynı anda iki talebi (aynı çocuk + öğretmen, iki dilim) tek açık
/// randevu bırakır. İlk istek öğretmen sırasındaki taze okumada bekletilir; ikinci istek sırayı beklemeden okuyamamalıdır (sıra
/// kaldırılınca kapıya girer ve test kırmızı olur). İkinci savunma: kısmi tekil dizin aynı dilime ikinci açık randevuyu veritabanında
/// reddeder ve ihlal "Bu saat artık dolu." iş hatasına çevrilir (dilim denetimi kör edilerek sınanır).
/// </summary>
[Trait("Category", "Integration")]
public sealed class AppointmentConcurrencyTests
{
    private readonly TestDatabase database;

    public AppointmentConcurrencyTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task TwoGuardians_SameSlotAtOnce_OnlyOnePending()
    {
        RaceGate slotGate = new();
        using OkulApiFactory factory = this.GatedFactory(slotGate, new RaceGate());
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await OpenedAsync(client, w.Class.Teacher, start: "14:00", end: "14:30", durationMinutes: 30);
        Guid slot = (await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali)).Single();

        slotGate.Enabled = true;
        var first = RequestAsync(client, w.AliMother.Token, slot, w.Ali);
        await slotGate.FirstEntered.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var second = RequestAsync(client, w.AyseMother.Token, slot, w.Ayse);
        var results = await Task.WhenAll(first, second);
        slotGate.Enabled = false;

        slotGate.SecondEnteredWhileFirstWaited.ShouldBe(false, "ikinci talep öğretmen sırasını beklemeden dilimi okudu");
        results.Count(r => r.Status == HttpStatusCode.OK).ShouldBe(1);
        results.Single(r => r.Status == HttpStatusCode.OK).Body.GetProperty("state").GetString().ShouldBe("Pending");
        var rejected = results.Single(r => r.Status != HttpStatusCode.OK);
        rejected.Status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(rejected.Body).ShouldBe("Bu saat artık dolu.");
        (await AppointmentRowsAsync(factory, w.Class.School.TenantId)).Count.ShouldBe(1);
    }

    [Fact]
    public async Task SameGuardian_TwoRequestsAtOnce_SingleOpenAppointment()
    {
        RaceGate openGate = new();
        using OkulApiFactory factory = this.GatedFactory(new RaceGate(), openGate);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await OpenedAsync(client, w.Class.Teacher, start: "14:00", end: "15:00", durationMinutes: 30);
        Guid[] free = await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali);

        openGate.Enabled = true;
        var first = RequestAsync(client, w.AliMother.Token, free[0], w.Ali);
        await openGate.FirstEntered.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var second = RequestAsync(client, w.AliMother.Token, free[1], w.Ali);
        var results = await Task.WhenAll(first, second);
        openGate.Enabled = false;

        openGate.SecondEnteredWhileFirstWaited.ShouldBe(false, "ikinci talep sırayı beklemeden açık randevuyu okudu");
        results.Count(r => r.Status == HttpStatusCode.OK).ShouldBe(1);
        Detail(results.Single(r => r.Status != HttpStatusCode.OK).Body).ShouldBe("Bu öğretmenle bu çocuk için zaten açık bir randevunuz var.");
        (await AppointmentRowsAsync(factory, w.Class.School.TenantId)).Count(a => a.Status == AppointmentStatus.Pending).ShouldBe(1);
    }

    [Fact]
    public async Task PartialUniqueIndex_SecondActiveOnSlot_RejectedAndTranslated()
    {
        using OkulApiFactory factory = this.GatedFactory(new RaceGate(), new RaceGate(), hideActiveOnSlot: true);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        await OpenedAsync(client, w.Class.Teacher, start: "14:00", end: "14:30", durationMinutes: 30);
        Guid slot = (await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali)).Single();
        await RequestedAsync(client, w.AliMother.Token, slot, w.Ali);

        // Dilim denetimi kör: ikinci talep uygulama kuralını geçer, veritabanının kısmi tekil dizinine takılır.
        var (status, body, _) = await RequestAsync(client, w.AyseMother.Token, slot, w.Ayse);

        status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(body).ShouldBe("Bu saat artık dolu.");
        (await AppointmentRowsAsync(factory, w.Class.School.TenantId)).Count.ShouldBe(1);

        // Doğrudan veritabanında da: aynı dilime ikinci açık (onay bekleyen) randevu yazılamaz; reddedilmiş olan yazılabilir.
        DbUpdateException error = await Should.ThrowAsync<DbUpdateException>(() => WithContextAsync(factory, async c =>
        {
            AvailabilitySlot row = await c.AvailabilitySlots.IgnoreQueryFilters().SingleAsync(s => s.Id == slot);
            c.Appointments.Add(Appointment.Request(row, w.AyseMother.UserId, w.Ayse, "doğrudan", Morning));
            await c.SaveChangesAsync();
        }));
        error.InnerException.ShouldBeOfType<PostgresException>().SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
        await WithContextAsync(factory, async c =>
        {
            AvailabilitySlot row = await c.AvailabilitySlots.IgnoreQueryFilters().SingleAsync(s => s.Id == slot);
            Appointment rejected = Appointment.Request(row, w.AyseMother.UserId, w.Ayse, "reddedilen", Morning);
            rejected.Reject(null, Morning);
            c.Appointments.Add(rejected);
            await c.SaveChangesAsync();
        });
    }

    private OkulApiFactory GatedFactory(RaceGate slot, RaceGate open, bool hideActiveOnSlot = false) =>
        AppointmentsFactory(this.database, configure: services =>
        {
            services.RemoveAll<IAppointmentRepository>();
            services.AddScoped<AppointmentRepository>();
            services.AddScoped<IAppointmentRepository>(sp => new GatedAppointmentRepository(sp.GetRequiredService<AppointmentRepository>(), slot, open, hideActiveOnSlot));
        });
}
