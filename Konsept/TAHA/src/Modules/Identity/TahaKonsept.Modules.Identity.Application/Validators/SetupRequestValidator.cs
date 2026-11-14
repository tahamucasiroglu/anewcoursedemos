using FluentValidation;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Validators;

/// <summary>
/// <see cref="SetupRequest"/> doğrulaması: kullanıcı adı 3-64 karakter, parola 8-128 karakter.
/// </summary>
public sealed class SetupRequestValidator : AbstractValidator<SetupRequest>
{
    /// <summary>Kuralları tanımlar.</summary>
    public SetupRequestValidator()
    {
        RuleFor(x => x.UserName)
            .Must(v => !string.IsNullOrWhiteSpace(v)).WithMessage("Kullanıcı adı gerekli.")
            .Must(v => (v ?? string.Empty).Trim().Length is >= 3 and <= 64).WithMessage("Kullanıcı adı 3-64 karakter olmalı.");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Parola gerekli.")
            .Length(8, 128).WithMessage("Parola 8-128 karakter olmalı.");
    }
}
