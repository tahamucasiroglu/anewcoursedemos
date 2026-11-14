using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Monitoring.Contracts.Abstract;

namespace Konsept.Modules.Identity.Application.Base;

/// <summary>Identity servislerinin ortak bağımlılıkları (kurucuları kısa tutar).</summary>
/// <param name="Users">Kullanıcı deposu.</param>
/// <param name="Sessions">Oturum deposu.</param>
/// <param name="Passwords">Şifre hash'leme.</param>
/// <param name="Tokens">Erişim token'ı üretimi.</param>
/// <param name="RefreshTokens">Refresh token üretimi.</param>
/// <param name="Events">Günlük olay yazıcısı (Monitoring Contracts).</param>
/// <param name="TimeProvider">Saat kaynağı.</param>
public sealed record AuthServiceDependencies(
    IUserRepository Users,
    ISessionRepository Sessions,
    IPasswordHashing Passwords,
    ITokenIssuer Tokens,
    IRefreshTokenFactory RefreshTokens,
    ISystemEventWriter Events,
    TimeProvider TimeProvider);
