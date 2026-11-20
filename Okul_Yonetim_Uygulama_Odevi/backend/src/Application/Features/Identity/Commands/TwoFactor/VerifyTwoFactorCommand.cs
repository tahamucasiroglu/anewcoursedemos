using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Identity.Commands.TwoFactor;

/// <summary>Girişte 6 haneli kodu doğrula; oturum açılır. Kod taşır, loglanmaz.</summary>
public sealed record VerifyTwoFactorCommand(string Challenge, string Code) : IRequest<AuthResult>;
