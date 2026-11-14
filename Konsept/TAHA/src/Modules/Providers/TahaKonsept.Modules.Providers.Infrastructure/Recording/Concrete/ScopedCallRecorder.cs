using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Base;
using TahaKonsept.Modules.Providers.Domain.Entities;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.Recording.Concrete;

/// <summary>
/// <see cref="CallRecorderBase"/>'in tekil uygulaması: her fiyat çözümü ve kayıt için yeni bir DI kapsamı (yeni DbContext) açar;
/// böylece paralel model çağrıları güvenle kaydedilir.
/// </summary>
public sealed class ScopedCallRecorder : CallRecorderBase
{
    private readonly IServiceScopeFactory _scopes;

    /// <summary>Kurucu.</summary>
    /// <param name="scopes">Kapsam fabrikası.</param>
    /// <param name="calculator">Maliyet hesaplayıcı.</param>
    /// <param name="logger">Logger.</param>
    public ScopedCallRecorder(IServiceScopeFactory scopes, ICostCalculator calculator, ILogger<ScopedCallRecorder> logger)
        : base(calculator, logger)
    {
        _scopes = scopes;
    }

    /// <inheritdoc />
    protected override async Task<ModelPricing> ResolvePricingAsync(Guid userId, ModelInfo model, CancellationToken ct)
    {
        await using var scope = _scopes.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IModelPriceResolver>().ResolveAsync(userId, model, ct);
    }

    /// <inheritdoc />
    protected override async Task SaveAsync(CallLog log, CancellationToken ct)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICallLogRepository>();
        await repository.AddAsync(log, ct);
        await repository.UnitOfWork.SaveChangesAsync(ct);
    }
}
