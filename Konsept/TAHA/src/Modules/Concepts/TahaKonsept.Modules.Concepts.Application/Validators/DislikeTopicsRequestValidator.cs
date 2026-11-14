using FluentValidation;
using TahaKonsept.Modules.Concepts.Domain.Constants;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Validators;

/// <summary>
/// <see cref="DislikeTopicsRequest"/> doğrulayıcısı: 1–100 dolu başlık.
/// </summary>
public sealed class DislikeTopicsRequestValidator : AbstractValidator<DislikeTopicsRequest>
{
    /// <summary>Kuralları tanımlar.</summary>
    public DislikeTopicsRequestValidator()
    {
        RuleFor(x => x.Titles).NotNull().WithMessage("Başlık listesi boş olamaz.")
            .Must(t => t is { Count: >= ConceptLimits.MinCount and <= ConceptLimits.MaxCount })
            .WithMessage($"{ConceptLimits.MinCount}–{ConceptLimits.MaxCount} başlık gönderin.");
        RuleForEach(x => x.Titles)
            .Must(t => !string.IsNullOrWhiteSpace(t)).WithMessage("Başlık boş olamaz.")
            .MaximumLength(ConceptLimits.TitleMaxLength).WithMessage("Başlık çok uzun.");
    }
}
