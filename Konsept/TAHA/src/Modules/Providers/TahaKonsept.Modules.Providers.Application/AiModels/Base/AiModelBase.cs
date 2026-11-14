using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.AiModels.Abstract;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.AiModels.Base;

/// <summary>
/// Metin/görsel/ses modellerinin ortak tabanı: katalog kaydı + kimlik bilgisi + bağımlılıklar ve
/// <see cref="ExecuteMeteredAsync{TOut}"/> (geçici hatalarda üstel bekleme ile yeniden deneme, her denemeyi CallLog'a maliyetiyle yazma).
/// </summary>
public abstract class AiModelBase : IAiModel
{
    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    protected AiModelBase(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        Credential = credential ?? throw new ArgumentNullException(nameof(credential));
        Dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
    }

    /// <summary>Katalog kaydı.</summary>
    public ModelInfo Model { get; }

    /// <inheritdoc />
    public string PlatformName => Model.Platform;

    /// <inheritdoc />
    public string ModelId => Model.ProviderModelId;

    /// <inheritdoc />
    public string ModelKey => Model.Key;

    /// <inheritdoc />
    public string DisplayName => Model.DisplayName;

    /// <inheritdoc />
    public ModelCapability Capability => Model.Capability;

    /// <summary>Kimlik bilgisi.</summary>
    protected ProviderCredential Credential { get; }

    /// <summary>Ortak bağımlılıklar.</summary>
    protected AiModelDependencies Dependencies { get; }

    /// <summary>Logger.</summary>
    protected ILogger Logger => Dependencies.Logger;

    /// <summary>Görünen platform adı (hata mesajları için).</summary>
    protected string PlatformDisplayName => ProviderPlatformDirectory.DisplayNameOf(Model.Platform);

    /// <summary>En fazla deneme sayısı (geçici hatalar için).</summary>
    protected virtual int MaxAttempts => 3;

    /// <summary>Denemeler arası bekleme (1. denemeden sonra 2 sn, sonra 4 sn).</summary>
    /// <param name="attempt">Biten deneme numarası (1'den başlar).</param>
    protected virtual TimeSpan RetryDelay(int attempt) => TimeSpan.FromSeconds(Math.Pow(2, attempt));

    /// <summary>Bir sağlayıcı çağrısını ölçerek çalıştırır: süre, kullanım, maliyet, CallLog, yeniden deneme.</summary>
    /// <typeparam name="TOut">Ham çıktı türü.</typeparam>
    /// <param name="context">Kullanıcı + amaç.</param>
    /// <param name="send">Tek HTTP çağrısı.</param>
    /// <param name="usageOf">Çıktıdan kullanımı okur.</param>
    /// <param name="ct">İptal belirteci.</param>
    protected async Task<Result<Metered<TOut>>> ExecuteMeteredAsync<TOut>(
        CallContext context,
        Func<CancellationToken, Task<Result<TOut>>> send,
        Func<TOut, CallUsage> usageOf,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(send);
        ArgumentNullException.ThrowIfNull(usageOf);
        for (var attempt = 1; ; attempt++)
        {
            var started = Stopwatch.GetTimestamp();
            Result<TOut> result;
            try
            {
                result = await send(ct);
            }
            catch (HttpRequestException ex)
            {
                result = ProviderHttpErrors.Network(PlatformDisplayName, ex.Message);
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                result = ProviderHttpErrors.Network(PlatformDisplayName, "zaman aşımı (" + ex.Message + ")");
            }

            var durationMs = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            var usage = result.IsSuccess ? usageOf(result.Value) : CallUsage.Empty;
            var cost = await Dependencies.Recorder.RecordAsync(
                new CallRecordRequest(context, Model, usage, durationMs, result.IsSuccess ? null : result.Error),
                CancellationToken.None);

            if (result.IsSuccess)
            {
                return new Metered<TOut>(result.Value, cost, durationMs, attempt);
            }

            if (!ProviderErrorCodes.IsTransient(result.Error.Code) || attempt >= MaxAttempts)
            {
                Logger.LogWarning("{Model} çağrısı başarısız ({Code}): {Message}", Model.Key, result.Error.Code, result.Error.Message);
                return result.Error;
            }

            Logger.LogInformation("{Model} geçici hata ({Code}), {Attempt}. deneme sonrası yeniden deneniyor.", Model.Key, result.Error.Code, attempt);
            await Task.Delay(RetryDelay(attempt), ct);
        }
    }
}
