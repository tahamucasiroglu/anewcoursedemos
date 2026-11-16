using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Base;
using Konsept.Shared.Kernel.Abstract;

namespace Konsept.Modules.AiGateway.Application.Concrete;

/// <summary>Deneme çağrısı servisi.</summary>
/// <param name="gateway">Çağrı kapısı.</param>
/// <param name="catalog">Katalog.</param>
/// <param name="currentUser">İsteği yapan kullanıcı.</param>
/// <param name="permissions">Yetkiler.</param>
public sealed class TrialCallService(IAiCallGateway gateway, ICatalogSource catalog, ICurrentUser currentUser, ICallerPermissions permissions)
    : TrialCallServiceBase(gateway, catalog, currentUser, permissions);
