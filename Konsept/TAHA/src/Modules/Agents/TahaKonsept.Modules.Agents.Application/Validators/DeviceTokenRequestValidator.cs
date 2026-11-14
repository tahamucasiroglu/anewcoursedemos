using FluentValidation;
using TahaKonsept.Shared.Contracts.Agent.Pairing;

namespace TahaKonsept.Modules.Agents.Application.Validators;

/// <summary>
/// <see cref="DeviceTokenRequest"/> doğrulaması: cihaz kodu zorunlu (≤ 128).
/// </summary>
public sealed class DeviceTokenRequestValidator : AbstractValidator<DeviceTokenRequest>
{
    /// <summary>Kuralları tanımlar.</summary>
    public DeviceTokenRequestValidator()
    {
        RuleFor(x => x.DeviceCode).NotEmpty().WithMessage("Cihaz kodu gerekli.").MaximumLength(128);
    }
}
