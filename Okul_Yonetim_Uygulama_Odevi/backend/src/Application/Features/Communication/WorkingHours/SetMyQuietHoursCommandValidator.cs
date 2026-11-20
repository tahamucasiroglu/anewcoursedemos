using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Domain.Communication;

namespace OkulYonetim.Application.Features.Communication.WorkingHours;

/// <summary>
/// Mesai saatleri (spec 0011 gereksinim 6, AC-9): sessiz mod açılıyorsa başlangıç ve bitiş "SS:DD" ve 15 dakikalık adımda,
/// başlangıç bitişten önce (aynı gün), en az bir geçerli gün. Kapatırken alan istenmez.
/// </summary>
public sealed class SetMyQuietHoursCommandValidator : AbstractValidator<SetMyQuietHoursCommand>
{
    public SetMyQuietHoursCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.Start).Must(s => QuietHoursText.TryParseTime(s, out _))
            .When(c => c.Enabled)
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.StartInvalid));
        this.RuleFor(c => c.End).Must(e => QuietHoursText.TryParseTime(e, out _))
            .When(c => c.Enabled)
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.EndInvalid));
        this.RuleFor(c => c.Start).Must((c, s) => QuietHoursText.TryParseTime(s, out TimeOnly start) && QuietHoursText.TryParseTime(c.End, out TimeOnly end) && start < end)
            .When(c => c.Enabled && QuietHoursText.TryParseTime(c.Start, out _) && QuietHoursText.TryParseTime(c.End, out _))
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.StartBeforeEnd));
        this.RuleFor(c => c.Days).Must(d => d is { Count: > 0 })
            .When(c => c.Enabled)
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.DaysRequired));
        this.RuleFor(c => c.Days).Must(d => QuietHoursText.TryParseDays(d, out WorkDays days) && days != WorkDays.None)
            .When(c => c.Enabled && c.Days is { Count: > 0 })
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.DaysInvalid));
    }
}
