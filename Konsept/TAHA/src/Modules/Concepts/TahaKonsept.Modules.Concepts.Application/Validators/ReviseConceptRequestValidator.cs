using FluentValidation;
using TahaKonsept.Modules.Concepts.Domain.Constants;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Validators;

/// <summary>
/// <see cref="ReviseConceptRequest"/> doğrulayıcısı: talimat dolu ve en fazla 4000 karakter; belge verilmişse geçerli olmalı.
/// </summary>
public sealed class ReviseConceptRequestValidator : AbstractValidator<ReviseConceptRequest>
{
    /// <summary>Kuralları tanımlar.</summary>
    public ReviseConceptRequestValidator()
    {
        RuleFor(x => x.Instruction)
            .Must(i => !string.IsNullOrWhiteSpace(i)).WithMessage("Ne düzeltilmesini istediğinizi yazın.")
            .MaximumLength(ConceptLimits.InstructionMaxLength).WithMessage($"Talimat en fazla {ConceptLimits.InstructionMaxLength} karakter olabilir.");
        RuleFor(x => x.Document!).SetValidator(new ConceptDocumentValidator()).When(x => x.Document is not null);
    }
}
