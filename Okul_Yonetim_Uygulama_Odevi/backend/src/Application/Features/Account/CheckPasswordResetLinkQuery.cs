using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Account;

/// <summary>Yenileme bağlantısı geçerli mi (sayfa açılırken). Anahtar taşır, loglanmaz.</summary>
public sealed record CheckPasswordResetLinkQuery(string Token) : IRequest<bool>;
