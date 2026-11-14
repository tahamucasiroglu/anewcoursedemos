using FluentValidation;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Validators;

/// <summary>
/// <see cref="MoveQueueItemRequest"/> doğrulayıcısı: yön -1 (yukarı) ya da +1 (aşağı).
/// </summary>
public sealed class MoveQueueItemRequestValidator : AbstractValidator<MoveQueueItemRequest>
{
    /// <summary>Kuralları kurar.</summary>
    public MoveQueueItemRequestValidator()
    {
        RuleFor(x => x.Direction).Must(d => d is -1 or 1).WithMessage("Yön -1 (yukarı) ya da 1 (aşağı) olmalı.");
    }
}
