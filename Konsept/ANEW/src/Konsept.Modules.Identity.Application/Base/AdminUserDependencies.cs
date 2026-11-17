using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Contracts.Abstract;
using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Shared.Kernel.Abstract;

namespace Konsept.Modules.Identity.Application.Base;

/// <summary>Admin kullanıcı servisinin bağımlılıkları (spec 0024; kurucuyu kısa tutar).</summary>
/// <param name="Users">Kullanıcı deposu.</param>
/// <param name="Bans">Ban kayıtları.</param>
/// <param name="Clips">Kullanıcı başına klip sayısı (Monitoring.Contracts; Production uygular).</param>
/// <param name="Sessions">Tüm oturumları iptal (0002 çıkış yeteneği: web, ajan, oturum sonu dinleyicileri).</param>
/// <param name="Events">Günlük olay yazıcısı.</param>
/// <param name="CurrentUser">İsteği yapan admin.</param>
/// <param name="TimeProvider">Saat.</param>
public sealed record AdminUserDependencies(
    IUserRepository Users,
    IUserBanRepository Bans,
    IClipStatistics Clips,
    ISessionRevoker Sessions,
    ISystemEventWriter Events,
    ICurrentUser CurrentUser,
    TimeProvider TimeProvider);
