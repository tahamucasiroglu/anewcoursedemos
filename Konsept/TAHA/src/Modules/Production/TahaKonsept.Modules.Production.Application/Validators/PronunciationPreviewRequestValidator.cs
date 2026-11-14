using FluentValidation;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Validators;

/// <summary>
/// <see cref="PronunciationPreviewRequest"/> doğrulayıcısı: metin zorunlu, en fazla 1000 karakter.
/// </summary>
public sealed class PronunciationPreviewRequestValidator : AbstractValidator<PronunciationPreviewRequest>
{
    /// <summary>Kuralları kurar.</summary>
    public PronunciationPreviewRequestValidator()
    {
        RuleFor(x => x.Text).NotEmpty().WithMessage("Dinlemek için metin yazın.").MaximumLength(1000).WithMessage("Önizleme metni en fazla 1000 karakter olabilir.");
    }
}
