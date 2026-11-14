using FluentValidation;
using TahaKonsept.Modules.Production.Domain.Constants;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Validators;

/// <summary>
/// <see cref="SavePronunciationRequest"/> doğrulayıcısı: yazılış ve okunuş zorunlu, en fazla 200 karakter.
/// </summary>
public sealed class SavePronunciationRequestValidator : AbstractValidator<SavePronunciationRequest>
{
    /// <summary>Kuralları kurar.</summary>
    public SavePronunciationRequestValidator()
    {
        RuleFor(x => x.Written).NotEmpty().WithMessage("Yazılış gerekli.").MaximumLength(ProductionLimits.PronunciationMaxLength);
        RuleFor(x => x.Spoken).NotEmpty().WithMessage("Okunuş gerekli.").MaximumLength(ProductionLimits.PronunciationMaxLength);
    }
}
