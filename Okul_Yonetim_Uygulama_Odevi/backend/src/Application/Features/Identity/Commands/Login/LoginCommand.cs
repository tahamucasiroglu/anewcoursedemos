using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Identity.Commands.Login;

/// <summary>
/// E-posta + şifre ile giriş. Bilinçli olarak <c>ILoggableRequest</c> değildir: istek şifre taşır, gövdesi loglanmaz (T9).
/// </summary>
public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResult>;
