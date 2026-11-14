using FluentValidation;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Validators;

/// <summary>
/// <see cref="RegenerateRequest"/> doğrulayıcısı: düzenlenmiş konsept verildiyse en az bir sahnesi olmalı; ayar verildiyse ayar kuralları.
/// </summary>
public sealed class RegenerateRequestValidator : AbstractValidator<RegenerateRequest>
{
    /// <summary>Kuralları kurar.</summary>
    public RegenerateRequestValidator()
    {
        RuleFor(x => x.Document!.Scenes).NotEmpty().When(x => x.Document is not null).WithMessage("Konseptte en az bir sahne olmalı.");
        RuleFor(x => x.Settings!).SetValidator(new ProductionSettingsDtoValidator()).When(x => x.Settings is not null);
    }
}
