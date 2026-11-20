using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Identity.Commands.CreateSchool;

/// <summary>
/// Okul açma (proje sahibinin komutu): okul + ilk şube + kurucu. Şifre yalnızca kişi sistemde yoksa gerekir.
/// Şifre taşıdığı için loglanmaz (T9). <c>Origin</c>: işlemi kimin yaptığı (komut satırı + işletim sistemi kullanıcısı); audit'e yazılır.
/// </summary>
public sealed record CreateSchoolCommand(string SchoolName, string BranchName, string FullName, string Email, string? Password, string? Origin = null)
    : IRequest<CreateSchoolResult>;
