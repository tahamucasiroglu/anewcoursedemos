using FluentValidation;
using TahaKonsept.Modules.Concepts.Domain.Constants;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Validators;

/// <summary>
/// <see cref="GenerateFromTopicsRequest"/> doğrulayıcısı: 1–100 başlık, her biri dolu ve en fazla 300 karakter.
/// </summary>
public sealed class GenerateFromTopicsRequestValidator : AbstractValidator<GenerateFromTopicsRequest>
{
    /// <summary>Kuralları tanımlar.</summary>
    public GenerateFromTopicsRequestValidator()
    {
        RuleFor(x => x.Titles).NotNull().WithMessage("Başlık listesi boş olamaz.")
            .Must(t => t is { Count: >= ConceptLimits.MinCount and <= ConceptLimits.MaxCount })
            .WithMessage($"{ConceptLimits.MinCount}–{ConceptLimits.MaxCount} başlık seçin.");
        RuleForEach(x => x.Titles)
            .Must(t => !string.IsNullOrWhiteSpace(t)).WithMessage("Başlık boş olamaz.")
            .MaximumLength(ConceptLimits.TitleMaxLength).WithMessage($"Başlık en fazla {ConceptLimits.TitleMaxLength} karakter olabilir.");
    }
}
