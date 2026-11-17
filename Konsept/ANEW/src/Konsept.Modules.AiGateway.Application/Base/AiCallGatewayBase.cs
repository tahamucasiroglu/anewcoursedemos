using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Modules.AiGateway.Application.Enums;
using Konsept.Modules.AiGateway.Domain.Entities;
using Konsept.Modules.AiGateway.Domain.Enums;
using Konsept.Modules.AiGateway.Domain.Rules;
using Konsept.Shared.Kernel.Results;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.AiGateway.Application.Base;

/// <summary>
/// Tek çağrı kapısı (spec 0013, H-1, H-3). Sağlayıcıya gitmeden önce sırayla: model katalogda mı → çağıranın model yetkisi
/// (BR-13) → çağıranın <b>kendi</b> key satırı (yalnızca <see cref="AiCaller.UserId"/> ile okunur, BR-1) ve çözülmesi → efor
/// (desteklenmiyorsa red, yoksa katalog varsayılanı) → çıktı sınırı (katalog limitini asla aşmaz). Sonra en fazla
/// <see cref="RetryRule.MaxAttempts"/> deneme: yalnızca 429/5xx ya da kurulamayan bağlantıda artan beklemeyle tekrar; zaman aşımı
/// (katalogdaki süre, <see cref="TimeProvider"/> ile) ve gönderildikten sonra kopan yanıt tekrar edilmez. Sağlayıcıya ulaşan her deneme
/// maliyetiyle kaydedilir; kurulamayan bağlantı kayıt üretmez. Harcama hiçbir çağrıyı engellemez (BR-12). Loglarda key ya da
/// sağlayıcının ham yanıtı yoktur.
/// </summary>
/// <param name="catalog">Katalog.</param>
/// <param name="keys">Key deposu.</param>
/// <param name="protector">Key çözme.</param>
/// <param name="provider">Sağlayıcı istemcisi.</param>
/// <param name="spend">Harcama deposu.</param>
/// <param name="time">Saat.</param>
/// <param name="logger">Log.</param>
public abstract class AiCallGatewayBase(
    ICatalogSource catalog,
    IApiKeyRepository keys,
    IApiKeyProtector protector,
    IProviderClient provider,
    ISpendRepository spend,
    TimeProvider time,
    ILogger logger) : IAiCallGateway
{
    private static readonly Action<ILogger, string, int, string, int, Exception?> AttemptFailed = LoggerMessage.Define<string, int, string, int>(
        LogLevel.Warning,
        new EventId(4101, "AiCallAttemptFailed"),
        "Model çağrısı denemesi başarısız: {Model} deneme {Attempt} ({Kind} {Status})");

    private static readonly Action<ILogger, string, int, string, Exception?> CallCompleted = LoggerMessage.Define<string, int, string>(
        LogLevel.Information,
        new EventId(4102, "AiCallCompleted"),
        "Model çağrısı tamamlandı: {Model} deneme {Attempt} ({Outcome})");

    /// <inheritdoc />
    public async Task<Result<AiCallResult>> CallAsync(AiCallRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var seed = catalog.Catalog;
        var model = seed.Find(request.ModelId); // Eski (kaldırılmış) kimlik yerine geçen girişe çözülür (spec 0026 Req 4).
        var entry = model is null ? null : seed.Providers.FirstOrDefault(p => string.Equals(p.Id, model.ProviderId, StringComparison.Ordinal));
        if (model is null || entry is null)
        {
            return Result<AiCallResult>.Failure(AiGatewayErrors.ModelNotFound);
        }

        if (!ModelPermission.Allows(request.Caller.Permissions, model.Id))
        {
            return Result<AiCallResult>.Failure(AiGatewayErrors.NoPermission);
        }

        var rows = await keys.ListAsync(request.Caller.UserId, cancellationToken).ConfigureAwait(false);
        var row = rows.FirstOrDefault(r => r.UserId == request.Caller.UserId && string.Equals(r.ProviderId, entry.Id, StringComparison.Ordinal));
        if (row is null)
        {
            return Result<AiCallResult>.Failure(AiGatewayErrors.NoKey(entry.Name));
        }

        if (protector.TryUnprotect(row.Ciphertext) is not { } apiKey)
        {
            return Result<AiCallResult>.Failure(AiGatewayErrors.KeyUnreadable(entry.Name));
        }

        var effort = request.Effort;
        if (effort is { } asked && !model.EffortLevels.Contains(asked, StringComparer.Ordinal))
        {
            if (string.Equals(model.Id, request.ModelId, StringComparison.Ordinal))
            {
                return Result<AiCallResult>.Failure(AiGatewayErrors.EffortUnsupported);
            }

            effort = null; // Eski kimlikle kaydedilmiş efor yerine geçen modelde yok: model varsayılanı (spec 0026 Req 4).
        }

        var isImage = model.Kind == ModelKind.Image;
        if ((isImage ? entry.ImageUrl : entry.ChatUrl) is null)
        {
            return Result<AiCallResult>.Failure(AiGatewayErrors.ModelNotCallable);
        }

        var call = new ProviderCall(
            entry,
            model,
            apiKey,
            effort ?? model.DefaultEffort,
            isImage ? null : ClampOutput(request.MaxOutputTokens, model.OutputTokenLimit),
            request.System,
            request.Prompt,
            Math.Max(1, request.ImageCount));
        return await RunAsync(request, call, cancellationToken).ConfigureAwait(false);
    }

    private static int? ClampOutput(int? requested, int? limit) => (requested, limit) switch
    {
        (null or <= 0, _) => limit, // 0 ya da negatif istek geçersizdir: sağlayıcıya gitmez, katalog limiti kullanılır (review r1 L-3).
        (_, null) => requested,
        ({ } r, { } l) => Math.Min(r, l),
    };

    private async Task<Result<AiCallResult>> RunAsync(AiCallRequest request, ProviderCall call, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var result = await SendOnceAsync(call, cancellationToken).ConfigureAwait(false);
            switch (result.Kind)
            {
                case ProviderAttemptKind.Completed:
                    {
                        var record = await RecordAsync(request, call, result).ConfigureAwait(false);
                        CallCompleted(logger, call.Model.Id, attempt, record.Outcome.ToString(), null);
                        return Result<AiCallResult>.Success(new AiCallResult
                        {
                            ModelId = call.Model.Id,
                            Text = result.Text,
                            Images = result.Images,
                            Outcome = record.Outcome,
                            Effort = call.Effort,
                            MaxOutputTokens = call.MaxOutputTokens,
                            InputTokens = record.InputTokens,
                            OutputTokens = record.OutputTokens,
                            CostUsd = record.CostUsd,
                            IsEstimated = record.IsEstimated,
                            Attempts = attempt,
                        });
                    }

                case ProviderAttemptKind.NoResponse:
                    // İstek gönderildi: sağlayıcı işlemiş ve ücretlendirmiş olabilir → tekrar yok, tahmini maliyetle kayıt (Req 8, 13).
                    // Çağıran vazgeçtiyse (istek iptali) de deneme sağlayıcıya ulaşmıştır: önce kayıt, sonra iptal yayılır.
                    await RecordAsync(request, call, result).ConfigureAwait(false);
                    AttemptFailed(logger, call.Model.Id, attempt, "no_response", 0, null);
                    cancellationToken.ThrowIfCancellationRequested();
                    return Result<AiCallResult>.Failure(AiGatewayErrors.ProviderNoResponse);

                case ProviderAttemptKind.HttpError:
                    {
                        var status = result.StatusCode ?? 0;
                        await RecordAsync(request, call, result).ConfigureAwait(false);
                        AttemptFailed(logger, call.Model.Id, attempt, "http", status, null);
                        if (!RetryRule.IsRetryableStatus(status) || attempt >= RetryRule.MaxAttempts)
                        {
                            return Result<AiCallResult>.Failure(AiGatewayErrors.ForProviderStatus(status));
                        }

                        break;
                    }

                default:
                    // Bağlantı hiç kurulamadı: istek sağlayıcıya ulaşmadı → kayıt yok, tekrar edilebilir (Req 7, 11).
                    AttemptFailed(logger, call.Model.Id, attempt, "connection", 0, null);
                    if (attempt >= RetryRule.MaxAttempts)
                    {
                        return Result<AiCallResult>.Failure(AiGatewayErrors.ProviderUnreachable);
                    }

                    break;
            }

            await Task.Delay(RetryRule.DelayAfter(attempt), time, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<ProviderAttempt> SendOnceAsync(ProviderCall call, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(call.Model.TimeoutSeconds), time);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        try
        {
            return await provider.SendAsync(call, linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return ProviderAttempt.Lost(); // Zaman aşımı ya da çağıranın iptali: istek gönderilmiş olabilir (Req 8, 11).
        }
    }

    private async Task<SpendRecord> RecordAsync(AiCallRequest request, ProviderCall call, ProviderAttempt result)
    {
        var model = call.Model;
        var isImage = model.Kind == ModelKind.Image;
        var outcome = result.Kind switch
        {
            ProviderAttemptKind.Completed => result.Truncated ? CallOutcome.Truncated : CallOutcome.Success,
            ProviderAttemptKind.HttpError => CallOutcome.ProviderError,
            _ => CallOutcome.NoResponse,
        };

        int? input = result.InputTokens;
        int? output = result.OutputTokens;
        int? images = isImage ? (result.Images.Count > 0 ? result.Images.Count : null) : null;
        decimal cost;
        var estimated = false;
        if (outcome == CallOutcome.ProviderError)
        {
            cost = 0m; // Hata yanıtı: istek işlenmemiş sayılır (Req 13).
        }
        else if (result.ReportedCostUsd is { } reported)
        {
            cost = reported; // Sağlayıcının bildirdiği gerçek maliyet esas alınır (Req 12).
        }
        else if (isImage)
        {
            estimated = images is null;
            images ??= call.ImageCount;
            cost = SpendCost.Image(model, images.Value);
        }
        else if (input is { } i && output is { } o)
        {
            cost = SpendCost.Text(model, i, o);
        }
        else
        {
            estimated = true;
            input ??= SpendCost.EstimateTokens((call.System ?? string.Empty) + call.Prompt);
            cost = SpendCost.Text(model, input.Value, output ?? 0);
        }

        var record = new SpendRecord
        {
            Id = Guid.CreateVersion7(),
            UserId = request.Caller.UserId,
            OccurredAt = time.GetUtcNow(),
            ProviderId = call.Provider.Id,
            ModelId = model.Id,
            Effort = call.Effort,
            Purpose = request.Purpose,
            ProfileId = request.ProfileId,
            InputTokens = input,
            OutputTokens = output,
            ImageCount = images,
            Outcome = outcome,
            CostUsd = cost,
            IsEstimated = estimated,
        };

        // Kayıt, çağıranın iptalinden bağımsız yazılır: sağlayıcıya ulaşan deneme kaydedilmeden kalmamalı (BR-12).
        await spend.AddAsync(record, CancellationToken.None).ConfigureAwait(false);
        return record;
    }
}
