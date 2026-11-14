using FluentValidation;
using TahaKonsept.Modules.Concepts.Domain.Constants;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Validators;

/// <summary>
/// <see cref="UpdateConceptRequest"/> doğrulayıcısı: başlık sınırı + belge (<see cref="ConceptDocumentValidator"/>).
/// </summary>
public sealed class UpdateConceptRequestValidator : AbstractValidator<UpdateConceptRequest>
{
    /// <summary>Kuralları tanımlar.</summary>
    public UpdateConceptRequestValidator()
    {
        RuleFor(x => x.Title).MaximumLength(ConceptLimits.TitleMaxLength).WithMessage($"Başlık en fazla {ConceptLimits.TitleMaxLength} karakter olabilir.");
        RuleFor(x => x.Document).NotNull().WithMessage("Konsept belgesi boş olamaz.").SetValidator(new ConceptDocumentValidator());
    }
}
