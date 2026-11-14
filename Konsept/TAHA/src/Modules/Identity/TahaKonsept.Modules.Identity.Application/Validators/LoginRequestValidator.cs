using FluentValidation;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Validators;

/// <summary>
/// <see cref="LoginRequest"/> doğrulaması: kullanıcı adı ve parola boş olamaz, makul uzunlukta olmalı.
/// </summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    /// <summary>Kuralları tanımlar.</summary>
    public LoginRequestValidator()
    {
        RuleFor(x => x.UserName)
            .Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage("Kullanıcı adı gerekli.")
            .MaximumLength(64).WithMessage("Kullanıcı adı en fazla 64 karakter olabilir.");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Parola gerekli.")
            .MaximumLength(128).WithMessage("Parola en fazla 128 karakter olabilir.");
    }
}
