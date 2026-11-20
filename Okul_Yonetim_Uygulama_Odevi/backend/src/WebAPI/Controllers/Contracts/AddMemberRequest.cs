using OkulYonetim.Application.Features.Administration;

namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Kişi ekleme isteği: ad-soyad, e-posta, isteğe bağlı telefon ve roller (rol + şubeler).</summary>
public sealed record AddMemberRequest(string FullName, string Email, string? Phone, IReadOnlyList<RoleInput> Roles);
