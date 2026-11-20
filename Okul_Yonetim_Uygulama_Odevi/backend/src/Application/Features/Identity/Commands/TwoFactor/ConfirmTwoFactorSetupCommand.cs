using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Identity.Commands.TwoFactor;

/// <summary>Kurulumu uygulamanın ürettiği kodla tamamla; oturum açılır. Kod taşır, loglanmaz.</summary>
public sealed record ConfirmTwoFactorSetupCommand(string Challenge, string Code) : IRequest<AuthResult>;
