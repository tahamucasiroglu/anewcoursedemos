using FluentValidation;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Validators;

/// <summary>
/// <see cref="RegisterRequest"/> doğrulaması: kurulumla aynı kurallar (<see cref="CredentialRuleExtensions"/>): kullanıcı adı 3-64, parola 8-128 karakter.
/// </summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    /// <summary>Kuralları tanımlar.</summary>
    public RegisterRequestValidator()
    {
        RuleFor(x => x.UserName).ValidUserName();
        RuleFor(x => x.Password).ValidNewPassword();
    }
}
