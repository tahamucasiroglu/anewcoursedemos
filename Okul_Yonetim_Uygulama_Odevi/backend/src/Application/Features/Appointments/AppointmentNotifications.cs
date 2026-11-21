using Microsoft.Extensions.Logging;
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
/// <para>
/// Review tur 1 R1-2: commit sonrası iki çağrı (<see cref="DeliverToGuardiansAsync"/>, <see cref="ToTeacherAsync"/>) hata verirse
/// (ör. öğretmenin mesai saatini okurken veritabanı hatası) randevu kaydedilmiş kalır ve istek başarılı döner: hata (iptal hariç)
/// yalnızca türü ve kimliklerle Warning olarak loglanır, sonuç "bildirim gitmedi" olur — <see cref="GuardianNotifier"/> ile aynı ilke.
/// </para>
/// </summary>
public sealed class AppointmentNotifications
{
    private readonly ISender sender;
    private readonly IAppointmentRepository appointments;
    private readonly SchoolCalendar calendar;
    private readonly ILocalizationService localization;
    private readonly ILogger<AppointmentNotifications> logger;

    public AppointmentNotifications(ISender sender, IAppointmentRepository appointments, SchoolCalendar calendar, ILocalizationService localization, ILogger<AppointmentNotifications> logger)
    {
        this.sender = sender;
        this.appointments = appointments;
        this.calendar = calendar;
        this.localization = localization;
        this.logger = logger;
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

    /// <summary>
    /// İşlem içinde yazılan veli bildirimlerini commit sonrası bildirim kapısına verir; gönderilebilen sayıyı döndürür (hata → 0).
    /// </summary>
    public async Task<int> DeliverToGuardiansAsync(SchoolNoticeResult notices, Appointment appointment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notices);
        ArgumentNullException.ThrowIfNull(appointment);
        try
        {
            return await this.sender.Send(new DeliverNotificationsCommand(notices.Pushes, []), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not OutOfMemoryException)
        {
            this.logger.LogWarning(
                "Veli randevu bildirimleri kapıya verilemedi ({ErrorType}): okul {TenantId}, randevu {AppointmentId}, öğrenci {StudentId}",
                exception.GetType().Name,
                appointment.TenantId,
                appointment.Id,
                appointment.StudentId);
            return 0;
        }
    }

    /// <summary>Öğretmene bildirim (commit sonrası; mesai dışında gitmez). Gönderildiyse <c>true</c>; hata → <c>false</c>.</summary>
    public async Task<bool> ToTeacherAsync(Appointment appointment, string templateKey, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(appointment);
        try
        {
            string text = AppointmentNoticeText.Render(
                await this.localization.AppointmentsTextAsync(templateKey).ConfigureAwait(false),
                string.Empty,
                this.calendar.TurkeyTime(appointment.StartsAt),
                null);
            return await this.sender.Send(
                new NotifyTeacherCommand(appointment.TeacherUserId, PushNotificationKind.Appointment, appointment.Id, text),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not OutOfMemoryException)
        {
            this.logger.LogWarning(
                "Öğretmene randevu bildirimi verilemedi ({ErrorType}): okul {TenantId}, randevu {AppointmentId}, öğretmen {TeacherId}",
                exception.GetType().Name,
                appointment.TenantId,
                appointment.Id,
                appointment.TeacherUserId);
            return false;
        }
    }
}
