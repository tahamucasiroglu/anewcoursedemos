using System.Globalization;
using FluentValidation;
using FluentValidation.Results;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Domain.Academics;

namespace OkulYonetim.Application.Features.Academics.Assessments;

/// <summary>
/// Puan kaydı (spec 0013 C-3, gereksinim 6; plan Risks 6): öğrenci listesi zorunlu, en fazla 500, her öğrenci bir kez; her puan
/// boş (girilmedi) ya da 0–100 arası tam sayı — -1, 101, ondalık ve sayı olmayan değer reddedilir. Hata alan yolunda öğrencinin
/// kimliğini taşır (<c>Grades[&lt;öğrenci&gt;].Score</c>); bir puan bile geçersizse istek işleyiciye ulaşmaz, hiçbiri kaydedilmez.
/// </summary>
public sealed class SaveGradesCommandValidator : AbstractValidator<SaveGradesCommand>
{
    public SaveGradesCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.Grades).NotNull().WithMessage(_ => localization.AcademicsText(AcademicsMessages.EntriesRequired));
        this.RuleFor(c => c.Grades!.Count).LessThanOrEqualTo(AcademicsMessages.MaxEntries)
            .When(c => c.Grades is not null)
            .WithMessage(_ => localization.AcademicsText(AcademicsMessages.EntriesTooMany));
        this.RuleFor(c => c.Grades)
            .Must(g => g!.All(x => x is not null) && g!.Select(x => x.StudentId).Distinct().Count() == g!.Count)
            .When(c => c.Grades is not null)
            .WithMessage(_ => localization.AcademicsText(AcademicsMessages.StudentRepeated));
        this.RuleForEach(c => c.Grades)
            .Custom((grade, context) =>
            {
                if (grade is not null && !TryParseScore(grade.Score, out _))
                {
                    context.AddFailure(new ValidationFailure(
                        string.Create(CultureInfo.InvariantCulture, $"Grades[{grade.StudentId:D}].Score"),
                        localization.AcademicsText(AcademicsMessages.ScoreInvalid)));
                }
            })
            .When(c => c.Grades is not null);
    }

    /// <summary>Yazılan puan → boş (girilmedi) ya da 0–100 arası tam sayı; yalnızca rakam (işaret, ondalık ve boşluk içi yok).</summary>
    public static bool TryParseScore(string? text, out int? score)
    {
        score = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        string clean = text.Trim();
        if (clean.Length > 3 || !clean.All(char.IsAsciiDigit))
        {
            return false;
        }

        int value = int.Parse(clean, NumberStyles.None, CultureInfo.InvariantCulture);
        if (!Grade.IsValidScore(value))
        {
            return false;
        }

        score = value;
        return true;
    }
}
