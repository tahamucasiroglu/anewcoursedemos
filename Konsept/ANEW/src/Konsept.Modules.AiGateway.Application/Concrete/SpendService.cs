using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Base;
using Konsept.Shared.Kernel.Abstract;

namespace Konsept.Modules.AiGateway.Application.Concrete;

/// <summary>Aylık harcama servisi.</summary>
/// <param name="spend">Harcama deposu.</param>
/// <param name="currentUser">İsteği yapan kullanıcı.</param>
/// <param name="time">Saat.</param>
public sealed class SpendService(ISpendRepository spend, ICurrentUser currentUser, TimeProvider time)
    : SpendServiceBase(spend, currentUser, time);
