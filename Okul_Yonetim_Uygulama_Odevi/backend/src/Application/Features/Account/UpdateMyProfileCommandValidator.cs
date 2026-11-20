using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Application.Features.Administration.Members;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Account;

/// <summary>Profil alanları: spec 0005'teki kişi formu kuralları (ad-soyad ≤ 100, Türkiye telefonu).</summary>
public sealed class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileCommandValidator(ILocalizationService localization) =>
        MemberFormRules.Apply(this, c => c.FullName, c => c.Phone, localization);
}
