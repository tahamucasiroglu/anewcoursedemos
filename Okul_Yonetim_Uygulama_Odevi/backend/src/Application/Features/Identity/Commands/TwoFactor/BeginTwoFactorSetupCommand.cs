using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Identity.Commands.TwoFactor;

/// <summary>Kurulum başlat: doğrulayıcı uygulamaya eklenecek anahtar. Bekleme anahtarı taşır, loglanmaz.</summary>
public sealed record BeginTwoFactorSetupCommand(string Challenge) : IRequest<TwoFactorSetupResult>;
