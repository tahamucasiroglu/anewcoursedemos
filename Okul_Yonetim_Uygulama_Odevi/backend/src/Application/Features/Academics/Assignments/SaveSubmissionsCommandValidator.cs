using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Domain.Academics;

namespace OkulYonetim.Application.Features.Academics.Assignments;

/// <summary>Teslim kaydı: öğrenci listesi zorunlu, en fazla 500, her öğrenci bir kez; durum <c>Unmarked</c> / <c>Submitted</c> / <c>NotSubmitted</c> (harf farkı gözetilmez, sayı kabul edilmez).</summary>
public sealed class SaveSubmissionsCommandValidator : AbstractValidator<SaveSubmissionsCommand>
{
    public SaveSubmissionsCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.Entries).NotNull().WithMessage(_ => localization.AcademicsText(AcademicsMessages.EntriesRequired));
        this.RuleFor(c => c.Entries!.Count).LessThanOrEqualTo(AcademicsMessages.MaxEntries)
            .When(c => c.Entries is not null)
            .WithMessage(_ => localization.AcademicsText(AcademicsMessages.EntriesTooMany));
        this.RuleFor(c => c.Entries)
            .Must(e => e!.All(x => x is not null) && e!.Select(x => x.StudentId).Distinct().Count() == e!.Count)
            .When(c => c.Entries is not null)
            .WithMessage(_ => localization.AcademicsText(AcademicsMessages.StudentRepeated));
        this.RuleForEach(c => c.Entries)
            .Must(e => e is not null && TryParseStatus(e.Status, out _))
            .When(c => c.Entries is not null)
            .WithMessage(_ => localization.AcademicsText(AcademicsMessages.StatusInvalid));
    }

    /// <summary>API'deki durum metni → durum.</summary>
    public static bool TryParseStatus(string? text, out SubmissionStatus status)
    {
        status = default;
        return text is not null
            && !int.TryParse(text, out _)
            && Enum.TryParse(text, ignoreCase: true, out status)
            && Enum.IsDefined(status);
    }
}
