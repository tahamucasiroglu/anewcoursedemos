using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Models;

namespace TahaKonsept.Modules.Production.Tests.Support;

/// <summary>
/// Sahte bütçe bekçisi: <see cref="Blocked"/> açıkken yeni üretimi 409 <c>providers.budget_exceeded</c> ile reddeder.
/// </summary>
public sealed class FakeBudgetGuard : IBudgetGuard
{
    /// <summary>Bütçe aşıldı mı.</summary>
    public bool Blocked { get; set; }

    /// <summary>Kontrol sayısı.</summary>
    public int Checks { get; private set; }

    /// <inheritdoc />
    public Task<BudgetStatus> GetStatusAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult(new BudgetStatus { LimitUsd = 10, SpentUsd = Blocked ? 11 : 1, StopOnExceed = true, WarnAtPercent = 80 });

    /// <inheritdoc />
    public Task<Result> EnsureCanStartProductionAsync(Guid userId, CancellationToken ct)
    {
        Checks++;
        return Task.FromResult(Blocked
            ? Result.Failure(Error.Conflict("providers.budget_exceeded", "Aylık bütçe aşıldı ($11.00 / $10.00)."))
            : Result.Success());
    }
}
