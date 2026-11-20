using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Account;

/// <summary>Şifremi unuttum isteği; yanıt her durumda aynı metindir. E-posta taşır, loglanmaz.</summary>
public sealed record RequestPasswordResetCommand(string Email) : IRequest<string>;
