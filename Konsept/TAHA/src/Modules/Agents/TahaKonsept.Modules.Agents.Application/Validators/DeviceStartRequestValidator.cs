using FluentValidation;
using TahaKonsept.Shared.Contracts.Agent.Pairing;

namespace TahaKonsept.Modules.Agents.Application.Validators;

/// <summary>
/// <see cref="DeviceStartRequest"/> doğrulaması: bilgisayar adı zorunlu (≤ 128), sürüm ≤ 64.
/// </summary>
public sealed class DeviceStartRequestValidator : AbstractValidator<DeviceStartRequest>
{
    /// <summary>Kuralları tanımlar.</summary>
    public DeviceStartRequestValidator()
    {
        RuleFor(x => x.MachineName).NotEmpty().WithMessage("Bilgisayar adı gerekli.").MaximumLength(128);
        RuleFor(x => x.AgentVersion).MaximumLength(64);
    }
}
