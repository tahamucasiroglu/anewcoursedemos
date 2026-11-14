using FluentValidation;
using TahaKonsept.Modules.Concepts.Domain.Constants;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Validators;

/// <summary>
/// <see cref="GenerateFromIdeaRequest"/> doğrulayıcısı: fikir 5–4000 karakter, adet 1–100.
/// </summary>
public sealed class GenerateFromIdeaRequestValidator : AbstractValidator<GenerateFromIdeaRequest>
{
    /// <summary>Kuralları tanımlar.</summary>
    public GenerateFromIdeaRequestValidator()
    {
        RuleFor(x => x.Idea)
            .Must(i => !string.IsNullOrWhiteSpace(i) && i.Trim().Length >= ConceptLimits.IdeaMinLength)
            .WithMessage("Biraz daha ayrıntılı bir fikir yazın.")
            .MaximumLength(ConceptLimits.IdeaMaxLength).WithMessage($"Fikir en fazla {ConceptLimits.IdeaMaxLength} karakter olabilir.");
        RuleFor(x => x.Count).InclusiveBetween(ConceptLimits.MinCount, ConceptLimits.MaxCount)
            .WithMessage($"Adet {ConceptLimits.MinCount}–{ConceptLimits.MaxCount} arasında olmalı.");
    }
}
