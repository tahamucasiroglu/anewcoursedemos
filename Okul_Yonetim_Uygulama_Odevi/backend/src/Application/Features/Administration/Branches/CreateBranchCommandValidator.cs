using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Administration.Branches;

/// <summary>Şube ekleme alanları (<see cref="BranchFormRules"/>).</summary>
public sealed class CreateBranchCommandValidator : AbstractValidator<CreateBranchCommand>
{
    public CreateBranchCommandValidator(ILocalizationService localization) =>
        BranchFormRules.Apply(this, c => c.Name, c => c.Address, c => c.Phone, localization);
}
