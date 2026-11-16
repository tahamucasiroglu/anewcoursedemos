using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Shared.Kernel.Abstract;

namespace Konsept.Modules.Identity.Application.Base;

/// <summary>Eşleştirme servisinin bağımlılıkları (kurucuyu kısa tutar).</summary>
/// <param name="Store">Kod deposu.</param>
/// <param name="Devices">Cihaz deposu.</param>
/// <param name="Revoker">Önceki cihazların iptali.</param>
/// <param name="Tokens">Rastgele belirteç ve hash üretimi (onay sırrı ve ajan oturum belirteci <c>knr_</c> önekli; her logda gizlenir).</param>
/// <param name="Events">Günlük olay yazıcısı.</param>
/// <param name="CurrentUser">İsteği yapan kullanıcı.</param>
/// <param name="TimeProvider">Saat kaynağı.</param>
public sealed record AgentPairingDependencies(
    IPairingStore Store,
    IAgentDeviceRepository Devices,
    IAgentDeviceRevoker Revoker,
    IRefreshTokenFactory Tokens,
    ISystemEventWriter Events,
    ICurrentUser CurrentUser,
    TimeProvider TimeProvider);
