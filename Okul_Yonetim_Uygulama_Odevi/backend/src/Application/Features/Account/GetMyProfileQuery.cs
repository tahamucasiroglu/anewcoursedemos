using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Account;

/// <summary>Kendi profilim (giriş gerekli; okul seçimi gerekmez).</summary>
public sealed record GetMyProfileQuery : IRequest<MyProfile>;
