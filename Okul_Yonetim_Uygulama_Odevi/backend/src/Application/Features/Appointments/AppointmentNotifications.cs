using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Communication;
using OkulYonetim.Application.Features.Communication.Notifications;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Appointments;
using OkulYonetim.Domain.Communication;

namespace OkulYonetim.Application.Features.Appointments;

/// <summary>
/// Randevu bildirimleri (spec 0016 C-5, gereksinim 7) — Appointments göndericiyi doğrudan çağırmaz; her şey Communication'ın bildirim
/// kapısından geçer. Veliye (onay, ret, öğretmen iptali): işlem içinde <see cref="NotifyStudentGuardiansCommand"/> ile randevunun
/// çocuğunun okulda etkin her velisine kayda bağlı "Okul bildirimi" (tür <see cref="GuardianNoticeKind.Appointment"/>; kaynak: onay/ret
/// için randevu, öğretmen iptali için kapanan dilim — 0013 tekil dizini veli + öğrenci + kaynak olduğundan), commit sonrası çağıran
/// <see cref="DeliverNotificationsCommand"/>'ı gönderir. Öğretmene (yeni talep, veli iptali): commit sonrası
/// <see cref="NotifyTeacherCommand"/> — mesai saatleri kapıda uygulanır. Metin öğretmen adı, gün, saat ve (veliye) varsa neden; konu yok.
/// </summary>
public sealed class AppointmentNotifications
{
    private readonly ISender sender;
    private readonly IAppointmentRepository appointments;
    private readonly SchoolCalendar calendar;
    private readonly ILocalizationService localization;

    public AppointmentNotifications(ISender sender, IAppointmentRepository appointments, SchoolCalendar calendar, ILocalizationService localization)
    {
        this.sender = sender;
        this.appointments = appointments;
        this.calendar = calendar;
        this.localization = localization;
    }

    /// <summary>Velilere okul bildirimi yazar (işlem içinde; kaydetmez, göndermez) ve commit sonrası verilecek bildirimleri döndürür.</summary>
    public async Task<SchoolNoticeResult> ToGuardiansAsync(Appointment appointment, string templateKey, string? reason, Guid sourceId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(appointment);
        string teacher = await this.appointments.PersonNameAsync(appointment.TeacherUserId, cancellationToken).ConfigureAwait(false);
        DateTimeOffset local = this.calendar.TurkeyTime(appointment.StartsAt);
        string text = AppointmentNoticeText.Render(
            await this.localization.AppointmentsTextAsync(templateKey).ConfigureAwait(false),
            teacher,
            local,
            reason);
        return await this.sender.Send(
            new NotifyStudentGuardiansCommand([appointment.StudentId], GuardianNoticeKind.Appointment, sourceId, DateOnly.FromDateTime(local.DateTime), text),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Öğretmene bildirim (commit sonrası; mesai dışında gitmez). Gönderildiyse <c>true</c>.</summary>
    public async Task<bool> ToTeacherAsync(Appointment appointment, string templateKey, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(appointment);
        string text = AppointmentNoticeText.Render(
            await this.localization.AppointmentsTextAsync(templateKey).ConfigureAwait(false),
            string.Empty,
            this.calendar.TurkeyTime(appointment.StartsAt),
            null);
        return await this.sender.Send(
            new NotifyTeacherCommand(appointment.TeacherUserId, PushNotificationKind.Appointment, appointment.Id, text),
            cancellationToken).ConfigureAwait(false);
    }
}
