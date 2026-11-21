using System.Net;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Appointments.AppointmentsTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Appointments;

/// <summary>
/// Spec 0016 AC-1: öğretmen 14:00–16:00 aralığını 20 dakikayla açınca 6 dilim oluşur (Türkiye saati → UTC), 14:00–14:45 / 20 dk → 2
/// dilim (kısa kuyruk dilim olmaz — plan A-2); geçmiş gün, 61. gün, başlangıç ≥ bitiş, 15 dakika adımı dışı saat (14:50 dahil), 8 saati
/// aşan aralık ve 15/20/30 dışı süre Türkçe alan hatası verir (gerçek gövde: <c>detail</c> + <c>Errors</c> dizisi — T20) ve hiçbir şey
/// oluşmaz; bugünün gelecek saati ve 60. gün kabul edilir; başlamış saatlere düşen dilim oluşmaz. Saat: 2026-11-20 Cuma 12:00 Türkiye.
/// </summary>
[Trait("Category", "Integration")]
public sealed class OpenAvailabilityTests
{
    private readonly TestDatabase database;

    public OpenAvailabilityTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Open_TwoHoursTwentyMinutes_SixSlotsInTurkeyTime()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        var (status, body, _) = await OpenAsync(client, w.Class.Teacher);

        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("slotCount").GetInt32().ShouldBe(6);
        JsonElement[] slots = [.. (await ScheduleAsync(client, w.Class.Teacher)).Body.GetProperty("slots").EnumerateArray()];
        slots.Length.ShouldBe(6);
        slots[0].GetProperty("startsAt").GetDateTimeOffset().ShouldBe(At1400);
        slots[5].GetProperty("endsAt").GetDateTimeOffset().ShouldBe(At1400.AddHours(2));
        slots.ShouldAllBe(s => s.GetProperty("state").GetString() == "Free");
        (await SlotRowsAsync(factory, w.Class.School.TenantId)).ShouldAllBe(s => s.TeacherUserId == w.Class.TeacherId && s.EndsAt - s.StartsAt == TimeSpan.FromMinutes(20));
    }

    [Fact]
    public async Task Open_ShortTail_NotASlot()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        (await OpenedAsync(client, w.Class.Teacher, start: "14:00", end: "14:45", durationMinutes: 20)).ShouldBe(2);
        (await SlotRowsAsync(factory, w.Class.School.TenantId)).Max(s => s.EndsAt).ShouldBe(At1400.AddMinutes(40));
    }

    [Theory]
    [InlineData("2026-11-19", "14:00", "16:00", 20, "Gün bugün ile 60 gün sonrası arasında olmalı")]
    [InlineData("2027-01-20", "14:00", "16:00", 20, "Gün bugün ile 60 gün sonrası arasında olmalı")]
    [InlineData(null, "14:00", "16:00", 20, "Gün zorunludur")]
    [InlineData(Day, "16:00", "14:00", 20, "Başlangıç saati bitişten önce olmalı")]
    [InlineData(Day, "14:00", "14:00", 20, "Başlangıç saati bitişten önce olmalı")]
    [InlineData(Day, "14:10", "16:00", 20, "Başlangıç saati SS:DD biçiminde ve 15 dakikalık adımlarla olmalı")]
    [InlineData(Day, "14:00", "14:50", 20, "Bitiş saati SS:DD biçiminde ve 15 dakikalık adımlarla olmalı")]
    [InlineData(Day, "2pm", "16:00", 20, "Başlangıç saati SS:DD biçiminde ve 15 dakikalık adımlarla olmalı")]
    [InlineData(Day, "08:00", "16:15", 20, "Aralık en fazla 8 saat olabilir")]
    [InlineData(Day, "14:00", "16:00", 25, "Görüşme süresi 15, 20 ya da 30 dakika olmalı")]
    [InlineData(Day, "14:00", "16:00", null, "Görüşme süresi 15, 20 ya da 30 dakika olmalı")]
    public async Task Open_InvalidField_TurkishFieldErrorAndNothingCreated(string? day, string start, string end, int? duration, string message)
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        var (status, body, _) = await OpenAsync(client, w.Class.Teacher, day, start, end, duration);

        status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(body).ShouldBe(message, "T20: ilk Türkçe alan mesajı detail'de");
        body.GetProperty("Errors").EnumerateArray().SelectMany(e => e.GetProperty("Errors").EnumerateArray()).Select(m => m.GetString()).ShouldContain(message);
        (await SlotRowsAsync(factory, w.Class.School.TenantId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Open_EightHoursTodayFutureAndSixtiethDay_Accepted()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        (await OpenedAsync(client, w.Class.Teacher, day: "2026-11-20", start: "13:00", end: "14:00", durationMinutes: 30)).ShouldBe(2);
        (await OpenedAsync(client, w.Class.Teacher, day: "2027-01-19", start: "08:00", end: "16:00", durationMinutes: 30)).ShouldBe(16);
    }

    [Fact]
    public async Task Open_Today_StartedSlotsSkipped_AllStartedIsBusinessError()
    {
        using OkulApiFactory factory = AppointmentsFactory(this.database);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        // Türkiye 12:00: 11:30 ve 12:00 başlamış (12:00 = şimdi), yalnızca 12:30 açılır.
        (await OpenedAsync(client, w.Class.Teacher, day: "2026-11-20", start: "11:30", end: "13:00", durationMinutes: 30)).ShouldBe(1);
        var (status, body, _) = await OpenAsync(client, w.Class.Teacher, day: "2026-11-20", start: "09:00", end: "11:30", durationMinutes: 30);

        status.ShouldBe(HttpStatusCode.BadRequest);
        Detail(body).ShouldBe("Bu aralıkta açılabilecek görüşme saati kalmadı.");
        (await SlotRowsAsync(factory, w.Class.School.TenantId)).Single().StartsAt.ShouldBe(new DateTimeOffset(2026, 11, 20, 9, 30, 0, TimeSpan.Zero));
    }
}
