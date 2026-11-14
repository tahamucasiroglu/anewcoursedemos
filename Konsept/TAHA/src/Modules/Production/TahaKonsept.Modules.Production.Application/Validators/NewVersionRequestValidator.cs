using FluentValidation;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Validators;

/// <summary>
/// <see cref="NewVersionRequest"/> doğrulayıcısı: ayar verildiyse ayar kuralları; not en fazla 200 karakter.
/// </summary>
public sealed class NewVersionRequestValidator : AbstractValidator<NewVersionRequest>
{
    /// <summary>Kuralları kurar.</summary>
    public NewVersionRequestValidator()
    {
        RuleFor(x => x.Settings!).SetValidator(new ProductionSettingsDtoValidator()).When(x => x.Settings is not null);
        RuleFor(x => x.Note).MaximumLength(200);
    }
}
