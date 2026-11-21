using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Domain.Appointments;

namespace OkulYonetim.Application.Features.Appointments.Availability;

/// <summary>
/// Müsaitlik açma (spec 0016 C-1, AC-1): gün zorunlu ve bugün (Türkiye günü, <see cref="SchoolCalendar"/>) ile 60 gün sonrası arasında;
/// başlangıç ve bitiş "SS:DD" ve 15 dakikalık adımda, başlangıç bitişten önce, aralık en fazla 8 saat; süre 15, 20 ya da 30 dakika.
/// Hata varsa hiçbir şey oluşmaz (400, alan listesi — T20).
/// </summary>
public sealed class OpenAvailabilityCommandValidator : AbstractValidator<OpenAvailabilityCommand>
{
    public OpenAvailabilityCommandValidator(ILocalizationService localization, SchoolCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(calendar);
        this.RuleFor(c => c.Day).NotNull()
            .WithMessage(_ => localization.AppointmentsText(AppointmentsMessages.DayRequired));
        this.RuleFor(c => c.Day).Must(d => AvailabilityPlanner.IsDayAllowed(d!.Value, calendar.Today()))
            .When(c => c.Day is not null)
            .WithMessage(_ => localization.AppointmentsText(AppointmentsMessages.DayOutOfRange));
        this.RuleFor(c => c.Start).Must(s => AppointmentTimeText.TryParse(s, out _))
            .WithMessage(_ => localization.AppointmentsText(AppointmentsMessages.StartInvalid));
        this.RuleFor(c => c.End).Must(e => AppointmentTimeText.TryParse(e, out _))
            .WithMessage(_ => localization.AppointmentsText(AppointmentsMessages.EndInvalid));
        this.RuleFor(c => c.Start).Must((c, s) => Times(c) is { } t && t.Start < t.End)
            .When(c => Times(c) is not null)
            .WithMessage(_ => localization.AppointmentsText(AppointmentsMessages.StartBeforeEnd));
        this.RuleFor(c => c.End).Must((c, e) => Times(c) is { } t && AvailabilityPlanner.IsRangeAllowed(t.Start, t.End))
            .When(c => Times(c) is { } t && t.Start < t.End)
            .WithMessage(_ => localization.AppointmentsText(AppointmentsMessages.RangeTooLong));
        this.RuleFor(c => c.DurationMinutes).Must(m => m is { } minutes && AvailabilityPlanner.IsDurationAllowed(minutes))
            .WithMessage(_ => localization.AppointmentsText(AppointmentsMessages.DurationInvalid));
    }

    private static (TimeOnly Start, TimeOnly End)? Times(OpenAvailabilityCommand command) =>
        AppointmentTimeText.TryParse(command.Start, out TimeOnly start) && AppointmentTimeText.TryParse(command.End, out TimeOnly end)
            ? (start, end)
            : null;
}
