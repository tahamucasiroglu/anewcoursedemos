using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Account;

/// <summary>Şifremi değiştir. Şifre taşır, loglanmaz.</summary>
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword, string NewPasswordRepeat) : IRequest<bool>;
