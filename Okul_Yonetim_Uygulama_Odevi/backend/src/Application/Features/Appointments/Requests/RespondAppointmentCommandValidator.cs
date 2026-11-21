using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Domain.Appointments;

namespace OkulYonetim.Application.Features.Appointments.Requests;

/// <summary>Talep yanıtı (spec 0016 gereksinim 5): yanıt <c>Approve</c> ya da <c>Reject</c>; neden isteğe bağlı, en fazla 300 karakter.</summary>
public sealed class RespondAppointmentCommandValidator : AbstractValidator<RespondAppointmentCommand>
{
    public const string Approve = "Approve";
    public const string Reject = "Reject";

    public RespondAppointmentCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.Decision).Must(d => d is Approve or Reject)
            .WithMessage(_ => localization.AppointmentsText(AppointmentsMessages.DecisionInvalid));
        this.RuleFor(c => c.Reason).Must(Appointment.IsReasonValid)
            .WithMessage(_ => localization.AppointmentsText(AppointmentsMessages.ReasonTooLong));
    }
}
