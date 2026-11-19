using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Identity.Queries.UserExists;

/// <summary>Okul açma komutu için: bu e-postayla bir kişi var mı (varsa şifre sorulmaz). Yalnızca komut satırından kullanılır.</summary>
public sealed record UserExistsQuery(string Email) : IRequest<bool>;
