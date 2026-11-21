using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Application.Features.Appointments;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Appointments;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Persistence.Repositories;
using OkulYonetim.Tests.Integration.Attendance;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;
using static OkulYonetim.Tests.Integration.Appointments.AppointmentsTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Communication.CommunicationTestData;

namespace OkulYonetim.Tests.Integration.Appointments;

/// <summary>
/// Spec 0016 AC-9: onay, ret (neden varsa ekli) ve öğretmen iptali bildirim kapısından randevunun çocuğunun velilerine (anne ve baba) okul
/// bildirimi olarak gereksinim 7 metniyle geçer; yeni talep ve veli iptali öğretmene mesai içinde (Türkiye saati, seçili gün, başlangıç dahil –
/// bitiş hariç) ya da sessiz mod tanımsızken bildirim üretir, mesai dışında üretmez ama bekleyen sayısı artar; bildirimde konu metni yoktur;
/// gönderici hata verirse işlem kaydedilir; işlem commit edilemezse hiçbir bildirim çıkmaz (bildirim yalnızca commit sonrası).
/// Saat: 2026-11-20 Cuma 12:00 Türkiye; görüşme 26.11.2026 14:00.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AppointmentNotificationTests
{
    private const string Topic = "Gizli görüşme konusu";
    private readonly TestDatabase database;

    public AppointmentNotificationTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Approve_BothParentsGetSchoolNoticeWithText()
    {
        CapturingPushNotifier push = new();
        using OkulApiFactory factory = AppointmentsFactory(this.database, push);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid id, _, string teacher) = await SetupAsync(client, w);

        (await RespondAsync(client, w.Class.Teacher, id, "Approve")).Status.ShouldBe(HttpStatusCode.OK);

        string expected = $"Randevunuz onaylandı — {teacher}, 26.11.2026 14:00";
        List<GuardianNotice> notices = await AppointmentNoticesAsync(factory, w.Ali);
        notices.Select(n => n.GuardianUserId).ShouldBe([w.AliMother.UserId, w.AliFather.UserId], ignoreOrder: true);
        notices.ShouldAllBe(n => n.Text == expected && n.SourceId == id && n.Day == new DateOnly(2026, 11, 26));
        foreach (ParentAccount parent in new[] { w.AliMother, w.AliFather })
        {
            PushNotification sent = push.To(parent.UserId).Single();
            sent.Kind.ShouldBe(PushNotificationKind.SchoolNotice);
            notices.Select(n => n.Id).ShouldContain(sent.SourceId);
            (await NoticesAsync(client, parent.Token)).Body.EnumerateArray().Select(n => n.GetProperty("text").GetString()).ShouldContain(expected);
        }

        push.To(w.AyseMother.UserId).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null, "Randevu talebiniz yanıtlanamadı — {0}, 26.11.2026 14:00")]
    [InlineData("Toplantım var", "Randevu talebiniz yanıtlanamadı — {0}, 26.11.2026 14:00. Neden: Toplantım var")]
    public async Task Reject_ParentsGetTextWithOptionalReason(string? reason, string format)
    {
        CapturingPushNotifier push = new();
        using OkulApiFactory factory = AppointmentsFactory(this.database, push);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid id, _, string teacher) = await SetupAsync(client, w);

        (await RespondAsync(client, w.Class.Teacher, id, "Reject", reason)).Status.ShouldBe(HttpStatusCode.OK);

        string expected = string.Format(System.Globalization.CultureInfo.InvariantCulture, format, teacher);
        (await AppointmentNoticesAsync(factory, w.Ali)).ShouldAllBe(n => n.Text == expected);
        (await AppointmentNoticesAsync(factory, w.Ali)).Count.ShouldBe(2);
        push.To(w.AliFather.UserId).Length.ShouldBe(1);
    }

    [Theory]
    [InlineData(null, "Randevunuz öğretmen tarafından iptal edildi — {0}, 26.11.2026 14:00")]
    [InlineData("Hastayım", "Randevunuz öğretmen tarafından iptal edildi — {0}, 26.11.2026 14:00. Neden: Hastayım")]
    public async Task TeacherCancel_ParentsGetNoticeAfterApprovalNotice(string? reason, string format)
    {
        CapturingPushNotifier push = new();
        using OkulApiFactory factory = AppointmentsFactory(this.database, push);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid id, Guid slot, string teacher) = await SetupAsync(client, w);
        (await RespondAsync(client, w.Class.Teacher, id, "Approve")).Status.ShouldBe(HttpStatusCode.OK);

        (await CancelAsync(client, w.Class.Teacher, id, reason)).Status.ShouldBe(HttpStatusCode.OK);

        string expected = string.Format(System.Globalization.CultureInfo.InvariantCulture, format, teacher);
        List<GuardianNotice> cancelled = [.. (await AppointmentNoticesAsync(factory, w.Ali)).Where(n => n.Text == expected)];
        cancelled.Count.ShouldBe(2);
        cancelled.ShouldAllBe(n => n.SourceId == slot, "öğretmen iptalinin kaynağı kapanan dilim (plan A-4)");
        push.To(w.AliMother.UserId).Length.ShouldBe(2, "onay + iptal");
    }

    [Fact]
    public async Task NewRequestAndGuardianCancel_TeacherNotifiedWithoutTopic()
    {
        CapturingPushNotifier push = new();
        using OkulApiFactory factory = AppointmentsFactory(this.database, push);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        (Guid id, _, _) = await SetupAsync(client, w);
        (await CancelAsync(client, w.AliMother.Token, id)).Status.ShouldBe(HttpStatusCode.OK);

        PushNotification[] toTeacher = push.To(w.Class.TeacherId);
        toTeacher.Select(p => p.Text).ShouldBe(["Yeni randevu talebi — 26.11.2026 14:00", "Randevu iptal edildi — 26.11.2026 14:00"]);
        toTeacher.ShouldAllBe(p => p.Kind == PushNotificationKind.Appointment && p.SourceId == id && p.TenantId == w.Class.School.TenantId);
        push.Sent.ShouldAllBe(p => p.Text == null || !p.Text.Contains(Topic, StringComparison.Ordinal));
        (await AppointmentNoticesAsync(factory, w.Ali)).ShouldBeEmpty("veli iptali velilere bildirim üretmez");
    }

    [Theory]
    [InlineData("08:00", "17:00", "Friday", true)]
    [InlineData("12:00", "13:00", "Friday", true)]
    [InlineData("08:00", "12:00", "Friday", false)]
    [InlineData("08:00", "17:00", "Monday", false)]
    public async Task NewRequest_TeacherNotifiedOnlyWithinWorkingHours(string start, string end, string day, bool within)
    {
        CapturingPushNotifier push = new();
        using OkulApiFactory factory = AppointmentsFactory(this.database, push);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (await SetQuietAsync(client, w.Class.Teacher, start, end, day)).Status.ShouldBe(HttpStatusCode.OK);

        (Guid id, _, _) = await SetupAsync(client, w);
        (await CancelAsync(client, w.AliMother.Token, id)).Status.ShouldBe(HttpStatusCode.OK);
        await RequestedAsync(client, w.AliMother.Token, (await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali))[0], w.Ali);

        push.To(w.Class.TeacherId).Length.ShouldBe(within ? 3 : 0);
        (await MeAsync(client, w.Class.Teacher)).GetProperty("pendingAppointmentCount").GetInt32().ShouldBe(1, "talep her durumda kaydedilir");
    }

    [Fact]
    public async Task NoQuietHours_LateEvening_TeacherNotified()
    {
        CapturingPushNotifier push = new();
        FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 20, 20, 0, 0, TimeSpan.Zero));
        using OkulApiFactory factory = AppointmentsFactory(this.database, push, time);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        await SetupAsync(client, w);

        push.To(w.Class.TeacherId).Length.ShouldBe(1, "Türkiye 23:00, sessiz mod tanımsız");
    }

    [Fact]
    public async Task ThrowingSender_OperationsStillSaved()
    {
        ThrowingPushNotifier push = new();
        using OkulApiFactory factory = AppointmentsFactory(this.database, push);
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);

        (Guid id, _, _) = await SetupAsync(client, w);
        (await RespondAsync(client, w.Class.Teacher, id, "Approve")).Status.ShouldBe(HttpStatusCode.OK);

        push.Attempts.ShouldBe(3, "öğretmen + iki veli");
        (await AppointmentRowsAsync(factory, w.Class.School.TenantId)).Single().Status.ShouldBe(AppointmentStatus.Approved);
        (await AppointmentNoticesAsync(factory, w.Ali)).Count.ShouldBe(2);
        factory.ReadLog().ShouldContain("Bildirim gönderilemedi");
    }

    [Fact]
    public async Task CommitFails_NoNotificationLeaves()
    {
        CapturingPushNotifier push = new();
        StrongBox<bool> fail = new(false);
        using OkulApiFactory factory = AppointmentsFactory(this.database, push, configure: services =>
        {
            services.RemoveAll<IUnitOfWork>();
            services.AddScoped<EfUnitOfWork>();
            services.AddScoped<IUnitOfWork>(sp => new FailingCommitUnitOfWork(
                sp.GetRequiredService<EfUnitOfWork>(),
                key => Volatile.Read(ref fail.Value) && key.StartsWith(AppointmentLock.KeyPrefix, StringComparison.Ordinal)));
        });
        using HttpClient client = factory.CreateClient();
        CommunicationWorld w = await WorldAsync(factory, client);
        (Guid id, Guid slot, _) = await SetupAsync(client, w);
        push.Sent.Clear();

        Volatile.Write(ref fail.Value, true);
        (await RespondAsync(client, w.Class.Teacher, id, "Approve")).Status.ShouldBe(HttpStatusCode.InternalServerError);
        Guid other = (await FreeSlotIdsAsync(client, w.AyseMother.Token, w.Class.TeacherId, w.Ayse)).First(s => s != slot);
        (await RequestAsync(client, w.AyseMother.Token, other, w.Ayse)).Status.ShouldBe(HttpStatusCode.InternalServerError);
        Volatile.Write(ref fail.Value, false);

        push.Sent.ShouldBeEmpty();
        (await AppointmentNoticesAsync(factory, w.Ali)).ShouldBeEmpty();
        (await AppointmentRowsAsync(factory, w.Class.School.TenantId)).Single().Status.ShouldBe(AppointmentStatus.Pending);
    }

    private static async Task<(Guid Id, Guid Slot, string Teacher)> SetupAsync(HttpClient client, CommunicationWorld w)
    {
        await OpenedAsync(client, w.Class.Teacher, start: "14:00", end: "15:00", durationMinutes: 30);
        string teacher = (await TeachersAsync(client, w.AliMother.Token, w.Ali)).Body[0].GetProperty("fullName").GetString()!;
        Guid slot = (await FreeSlotIdsAsync(client, w.AliMother.Token, w.Class.TeacherId, w.Ali))[0];
        return (await RequestedAsync(client, w.AliMother.Token, slot, w.Ali, Topic), slot, teacher);
    }
}
