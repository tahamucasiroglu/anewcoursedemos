using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Identity.Commands.RefreshSession;

/// <summary>Yenileme anahtarıyla yeni erişim anahtarı alır; yenileme anahtarı her seferinde değişir. Loglanmaz (anahtar taşır).</summary>
public sealed record RefreshSessionCommand(string RefreshToken) : IRequest<AuthResult>;
