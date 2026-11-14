using FluentValidation;
using TahaKonsept.Shared.Contracts.Agent.Pairing;

namespace TahaKonsept.Modules.Agents.Application.Validators;

/// <summary>
/// <see cref="DeviceApproveRequest"/> doğrulaması: kod <c>XXXX-XXXX</c> (tire/boşluk isteğe bağlı, harf/rakam, 8 karakter).
/// </summary>
public sealed partial class DeviceApproveRequestValidator : AbstractValidator<DeviceApproveRequest>
{
    /// <summary>Kuralları tanımlar.</summary>
    public DeviceApproveRequestValidator()
    {
        RuleFor(x => x.UserCode)
            .NotEmpty().WithMessage("Eşleme kodunu girin.")
            .Matches(UserCodePattern()).WithMessage("Kod XXXX-XXXX biçiminde olmalı.");
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^\s*[A-Za-z0-9]{4}[\s-]?[A-Za-z0-9]{4}\s*$")]
    private static partial System.Text.RegularExpressions.Regex UserCodePattern();
}
