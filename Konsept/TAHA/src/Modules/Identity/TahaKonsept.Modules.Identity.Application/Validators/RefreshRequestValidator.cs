using FluentValidation;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Validators;

/// <summary>
/// <see cref="RefreshRequest"/> doğrulaması: refresh token boş olamaz.
/// </summary>
public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    /// <summary>Kuralları tanımlar.</summary>
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Yenileme anahtarı gerekli.")
            .MaximumLength(512).WithMessage("Yenileme anahtarı geçersiz.");
    }
}
