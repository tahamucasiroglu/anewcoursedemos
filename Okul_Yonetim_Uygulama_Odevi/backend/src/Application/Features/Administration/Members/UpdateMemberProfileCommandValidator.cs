using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Administration.Members;

/// <summary>Profil düzenleme alanları (<see cref="MemberFormRules"/>).</summary>
public sealed class UpdateMemberProfileCommandValidator : AbstractValidator<UpdateMemberProfileCommand>
{
    public UpdateMemberProfileCommandValidator(ILocalizationService localization) =>
        MemberFormRules.Apply(this, c => c.FullName, c => c.Phone, localization);
}
