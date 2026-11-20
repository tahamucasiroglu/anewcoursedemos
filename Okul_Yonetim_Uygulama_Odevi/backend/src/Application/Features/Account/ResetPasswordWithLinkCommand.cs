using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Account;

/// <summary>Bağlantıyla yeni şifre belirle. Anahtar ve şifre taşır, loglanmaz.</summary>
public sealed record ResetPasswordWithLinkCommand(string Token, string NewPassword, string NewPasswordRepeat) : IRequest<bool>;
