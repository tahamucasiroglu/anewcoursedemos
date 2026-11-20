using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Administration.Branches;

/// <summary>Şube düzenleme alanları (<see cref="BranchFormRules"/>).</summary>
public sealed class UpdateBranchCommandValidator : AbstractValidator<UpdateBranchCommand>
{
    public UpdateBranchCommandValidator(ILocalizationService localization) =>
        BranchFormRules.Apply(this, c => c.Name, c => c.Address, c => c.Phone, localization);
}
