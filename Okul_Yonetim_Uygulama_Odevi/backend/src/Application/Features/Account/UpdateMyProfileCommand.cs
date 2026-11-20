using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Account;

/// <summary>Kendi ad-soyad ve telefonumu değiştir. Kişisel veri taşır, loglanmaz.</summary>
public sealed record UpdateMyProfileCommand(string FullName, string? Phone) : IRequest<MyProfile>;
