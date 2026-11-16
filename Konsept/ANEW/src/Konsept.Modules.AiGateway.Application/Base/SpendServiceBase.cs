using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Modules.AiGateway.Domain.Rules;
using Konsept.Shared.Kernel.Abstract;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.AiGateway.Application.Base;

/// <summary>
/// Aylık harcama (spec 0013 Req 15–17): isteği yapan kullanıcının içinde bulunulan İstanbul takvim ayındaki tüm kayıtlarının
/// (tüm profiller, silinmiş key'lerin sağlayıcıları dahil) yuvarlanmamış toplamı. Yalnızca bilgi; hiçbir eşik yoktur.
/// </summary>
/// <param name="spend">Harcama deposu.</param>
/// <param name="currentUser">İsteği yapan kullanıcı.</param>
/// <param name="time">Saat.</param>
public abstract class SpendServiceBase(ISpendRepository spend, ICurrentUser currentUser, TimeProvider time) : ISpendService
{
    /// <inheritdoc />
    public async Task<Result<MonthlySpendDto>> GetMonthlyAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<MonthlySpendDto>.Failure(AiGatewayErrors.SignInRequired);
        }

        var (start, end) = IstanbulMonth.Of(time.GetUtcNow());
        var total = await spend.SumAsync(userId, start, end, cancellationToken).ConfigureAwait(false);
        return Result<MonthlySpendDto>.Success(new MonthlySpendDto(total, start));
    }
}
