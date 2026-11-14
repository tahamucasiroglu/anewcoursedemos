using Microsoft.Extensions.Logging;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Domain.Entities;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Base;

/// <summary>
/// Çağrı kaydetme akışı: fiyatı çöz → maliyeti hesapla → <see cref="CallLog"/> oluştur → kaydet. Kayıt hatası çağrıyı
/// bozmaz (loglanır, katalog fiyatıyla maliyet yine döner). Somut sınıf fiyat çözümünü ve kalıcılığı sağlar.
/// </summary>
public abstract class CallRecorderBase : ICallRecorder
{
    /// <summary>Kurucu.</summary>
    /// <param name="calculator">Maliyet hesaplayıcı.</param>
    /// <param name="logger">Logger.</param>
    protected CallRecorderBase(ICostCalculator calculator, ILogger logger)
    {
        Calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Maliyet hesaplayıcı.</summary>
    protected ICostCalculator Calculator { get; }

    /// <summary>Logger.</summary>
    protected ILogger Logger { get; }

    /// <inheritdoc />
    public async Task<decimal> RecordAsync(CallRecordRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var cost = Calculator.Calculate(request.Model.Pricing, request.Usage);
        try
        {
            var pricing = await ResolvePricingAsync(request.Context.UserId, request.Model, ct);
            cost = Calculator.Calculate(pricing, request.Usage);
            var (units, unitKind) = UnitsOf(request.Usage);
            var log = CallLog.Create(
                request.Context.UserId,
                request.Model.Platform,
                request.Model.Key,
                request.Model.ProviderModelId,
                request.Context.Purpose,
                request.Usage.InputTokens,
                request.Usage.OutputTokens,
                request.Usage.CacheReadTokens,
                request.Usage.CacheWriteTokens,
                units,
                unitKind,
                cost,
                request.DurationMs,
                request.Error is null,
                request.Error?.Code,
                request.Error?.Message);
            await SaveAsync(log, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogError(ex, "Çağrı günlüğü yazılamadı ({Model}, {Purpose}).", request.Model.Key, request.Context.Purpose);
        }

        return cost;
    }

    /// <summary>Geçerli fiyatı çözer.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="ct">İptal belirteci.</param>
    protected abstract Task<ModelPricing> ResolvePricingAsync(Guid userId, ModelInfo model, CancellationToken ct);

    /// <summary>Kaydı kalıcı yazar.</summary>
    /// <param name="log">Kayıt.</param>
    /// <param name="ct">İptal belirteci.</param>
    protected abstract Task SaveAsync(CallLog log, CancellationToken ct);

    /// <summary>Token dışı birimi seçer.</summary>
    /// <param name="usage">Kullanım.</param>
    private static (decimal Units, string? Kind) UnitsOf(CallUsage usage) =>
        usage switch
        {
            { Images: > 0 } => (usage.Images, "image"),
            { Characters: > 0 } => (usage.Characters / 1000m, "kchars"),
            { AudioSeconds: > 0 } => (usage.AudioSeconds / 60m, "minute"),
            { WebSearchRequests: > 0 } => (usage.WebSearchRequests, "search"),
            _ => (0m, null),
        };
}
