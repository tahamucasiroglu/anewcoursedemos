using FluentValidation;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Validators;

/// <summary>
/// <see cref="AddToQueueRequest"/> doğrulayıcısı: 1-200 konsept, boş kimlik yok; ayar verildiyse ayar kuralları.
/// </summary>
public sealed class AddToQueueRequestValidator : AbstractValidator<AddToQueueRequest>
{
    /// <summary>Kuralları kurar.</summary>
    public AddToQueueRequestValidator()
    {
        RuleFor(x => x.ConceptIds).NotEmpty().WithMessage("En az bir konsept seçin.")
            .Must(ids => ids.Count <= 200).WithMessage("Tek seferde en fazla 200 konsept eklenebilir.");
        RuleForEach(x => x.ConceptIds).NotEqual(Guid.Empty).WithMessage("Geçersiz konsept kimliği.");
        RuleFor(x => x.Settings!).SetValidator(new ProductionSettingsDtoValidator()).When(x => x.Settings is not null);
    }
}
