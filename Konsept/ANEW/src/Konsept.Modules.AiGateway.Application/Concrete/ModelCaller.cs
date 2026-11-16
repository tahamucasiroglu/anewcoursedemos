using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Base;
using Konsept.Shared.Kernel.Abstract;

namespace Konsept.Modules.AiGateway.Application.Concrete;

/// <summary>Metin modeli çağrısı (AiGateway.Contracts, spec 0014); davranış <see cref="ModelCallerBase"/>'tedir.</summary>
/// <param name="gateway">Çağrı kapısı.</param>
/// <param name="currentUser">İsteği yapan kullanıcı.</param>
/// <param name="permissions">Yetkiler.</param>
public sealed class ModelCaller(IAiCallGateway gateway, ICurrentUser currentUser, ICallerPermissions permissions)
    : ModelCallerBase(gateway, currentUser, permissions);
