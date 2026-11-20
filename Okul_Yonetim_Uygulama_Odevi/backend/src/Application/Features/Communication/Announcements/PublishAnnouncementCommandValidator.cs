using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Domain.Communication;

namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>
/// Duyuru alanları (spec 0011 gereksinim 1, 4; AC-1, AC-5): başlık zorunlu, kırpılmış en fazla 150; metin zorunlu, en fazla
/// 4000; kapsam okul/şube/sınıf/öğrenci, okul dışında kapsam kimliği zorunlu. "Onay iste" seçiliyse soru zorunlu, en fazla
/// 300 ve son gün yarından en geç 180 gün sonrasına (Türkiye günü). Alan hatası varsa hiçbir şey oluşmaz.
/// </summary>
public sealed class PublishAnnouncementCommandValidator : AbstractValidator<PublishAnnouncementCommand>
{
    public PublishAnnouncementCommandValidator(ILocalizationService localization, SchoolCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(calendar);
        this.RuleFor(c => c.Title).Must(t => !string.IsNullOrWhiteSpace(t))
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.TitleRequired));
        this.RuleFor(c => c.Title).Must(t => t!.Trim().Length <= Announcement.TitleMaxLength)
            .When(c => !string.IsNullOrWhiteSpace(c.Title))
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.TitleTooLong));
        this.RuleFor(c => c.Body).Must(b => !string.IsNullOrWhiteSpace(b))
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.BodyRequired));
        this.RuleFor(c => c.Body).Must(b => b!.Trim().Length <= Announcement.BodyMaxLength)
            .When(c => !string.IsNullOrWhiteSpace(c.Body))
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.BodyTooLong));
        this.RuleFor(c => c.Scope).Must(IsScope)
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.ScopeInvalid));
        this.RuleFor(c => c.ScopeId).NotNull()
            .When(c => IsScope(c.Scope) && c.Scope != nameof(AnnouncementScope.School))
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.ScopeIdRequired));
        this.RuleFor(c => c.ConsentQuestion).Must(q => !string.IsNullOrWhiteSpace(q))
            .When(c => c.RequestConsent)
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.QuestionRequired));
        this.RuleFor(c => c.ConsentQuestion).Must(q => q!.Trim().Length <= Announcement.QuestionMaxLength)
            .When(c => c.RequestConsent && !string.IsNullOrWhiteSpace(c.ConsentQuestion))
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.QuestionTooLong));
        this.RuleFor(c => c.ConsentDeadline).NotNull()
            .When(c => c.RequestConsent)
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.DeadlineRequired));
        this.RuleFor(c => c.ConsentDeadline).Must(d => IsDeadlineInRange(d!.Value, calendar.Today()))
            .When(c => c.RequestConsent && c.ConsentDeadline is not null)
            .WithMessage(_ => localization.CommunicationText(CommunicationMessages.DeadlineRange));
    }

    /// <summary>Son gün yarından (dahil) en geç <see cref="Announcement.MaxDeadlineDays"/> gün sonrasına (dahil) mı.</summary>
    public static bool IsDeadlineInRange(DateOnly deadline, DateOnly today) =>
        deadline > today && deadline <= today.AddDays(Announcement.MaxDeadlineDays);

    private static bool IsScope(string? scope) =>
        scope is nameof(AnnouncementScope.School) or nameof(AnnouncementScope.Branch) or nameof(AnnouncementScope.Classroom) or nameof(AnnouncementScope.Student);
}
