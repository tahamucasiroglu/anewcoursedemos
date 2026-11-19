using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Identity.Commands.Logout;

/// <summary>Oturumu kapatır (giriş yapmış kişinin şu anki oturumu).</summary>
public sealed record LogoutCommand : IRequest<bool>;
