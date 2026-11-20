using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Identity.Commands.PlatformAdmin;

/// <summary>
/// Platform yöneticisi komutları (spec 0007, yalnızca komut satırından): yetki ver / al, iki adımlı doğrulamayı sıfırla.
/// <c>Origin</c>: komutu kim çalıştırdı (audit).
/// </summary>
public sealed record PlatformAdminCommand(string Email, PlatformAdminAction Action, string Origin) : IRequest<bool>;
