using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Domain.Appointments;

namespace OkulYonetim.Application.Features.Appointments.Requests;

/// <summary>Randevu talebi (spec 0016 gereksinim 4, AC-5): dilim ve çocuk zorunlu; konu boş olamaz, kırpılınca en fazla 300 karakter.</summary>
public sealed class RequestAppointmentCommandValidator : AbstractValidator<RequestAppointmentCommand>
{
    public RequestAppointmentCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.SlotId).Must(id => id is { } value && value != Guid.Empty)
            .WithMessage(_ => localization.AppointmentsText(AppointmentsMessages.SlotRequired));
        this.RuleFor(c => c.StudentId).Must(id => id is { } value && value != Guid.Empty)
            .WithMessage(_ => localization.AppointmentsText(AppointmentsMessages.StudentRequired));
        this.RuleFor(c => c.Topic).Must(t => !string.IsNullOrWhiteSpace(t))
            .WithMessage(_ => localization.AppointmentsText(AppointmentsMessages.TopicRequired));
        this.RuleFor(c => c.Topic).Must(Appointment.IsTopicValid)
            .When(c => !string.IsNullOrWhiteSpace(c.Topic))
            .WithMessage(_ => localization.AppointmentsText(AppointmentsMessages.TopicTooLong));
    }
}
