using FluentValidation;
using TahaKonsept.Modules.Concepts.Domain.Constants;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Validators;

/// <summary>
/// <see cref="SuggestTopicsRequest"/> doğrulayıcısı: adet 1–100, yön en fazla 300 karakter.
/// </summary>
public sealed class SuggestTopicsRequestValidator : AbstractValidator<SuggestTopicsRequest>
{
    /// <summary>Kuralları tanımlar.</summary>
    public SuggestTopicsRequestValidator()
    {
        RuleFor(x => x.Count).InclusiveBetween(ConceptLimits.MinCount, ConceptLimits.MaxCount)
            .WithMessage($"Adet {ConceptLimits.MinCount}–{ConceptLimits.MaxCount} arasında olmalı.");
        RuleFor(x => x.Hint).MaximumLength(ConceptLimits.HintMaxLength)
            .WithMessage($"Yön en fazla {ConceptLimits.HintMaxLength} karakter olabilir.");
    }
}
