using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Domain.Appointments;

namespace OkulYonetim.Application.Features.Appointments.Requests;

/// <summary>Randevu iptali (spec 0016 gereksinim 6): neden isteğe bağlı, en fazla 300 karakter (yalnızca öğretmen iptalinde saklanır).</summary>
public sealed class CancelAppointmentCommandValidator : AbstractValidator<CancelAppointmentCommand>
{
    public CancelAppointmentCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.Reason).Must(Appointment.IsReasonValid)
            .WithMessage(_ => localization.AppointmentsText(AppointmentsMessages.ReasonTooLong));
    }
}
