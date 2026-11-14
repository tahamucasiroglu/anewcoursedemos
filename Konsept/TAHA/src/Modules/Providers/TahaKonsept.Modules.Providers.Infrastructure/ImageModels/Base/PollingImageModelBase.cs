using System.Diagnostics;
using System.Text.Json;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Infrastructure.ImageModels.Models;
using TahaKonsept.Modules.Providers.Infrastructure.Media.Common;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.ImageModels.Base;

/// <summary>
/// Asenkron (gönder + sorgula) görsel API'lerinin ortak tabanı (BFL, Replicate): <see cref="SubmitAsync"/> işi başlatıp durum
/// adresini verir; taban <see cref="MediaModelOptions.PollInterval"/> aralıkla durum adresini sorgular (<see cref="ApplyAuth"/> ile
/// kimlik eklenir), <see cref="ParseStatus"/> ile çözer, tamamlanınca sonuç URL'lerini indirir. Süre aşımı
/// <c>providers.empty_response</c> (yeniden denenmez; iş zaten ücretlenmiş olabilir). Önemli üye: <see cref="RunJobAsync"/>.
/// </summary>
public abstract class PollingImageModelBase : HttpImageModelBase
{
    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="http">HTTP istemcisi.</param>
    /// <param name="options">Zamanlama ayarları.</param>
    protected PollingImageModelBase(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies, HttpClient http, MediaModelOptions options)
        : base(model, credential, dependencies, http, options)
    {
    }

    /// <inheritdoc />
    protected override Task<Result<ImageModelOutput>> GenerateCoreAsync(ImageRequest request, CancellationToken ct) =>
        RunJobAsync(request, ct);

    /// <summary>İşi başlatır.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Durum sorgu adresi.</returns>
    protected abstract Task<Result<Uri>> SubmitAsync(ImageRequest request, CancellationToken ct);

    /// <summary>Durum sorgusuna kimlik başlığını ekler.</summary>
    /// <param name="request">İstek.</param>
    protected abstract void ApplyAuth(HttpRequestMessage request);

    /// <summary>Durum yanıtını çözer.</summary>
    /// <param name="root">Yanıt kökü.</param>
    protected abstract ImagePollStatus ParseStatus(JsonElement root);

    /// <summary>Durum sorgusunda art arda tolere edilen geçici hata sayısı (tek GET yeniden denenir, iş yeniden gönderilmez).</summary>
    protected virtual int MaxConsecutivePollFailures => 3;

    /// <summary>
    /// Tek işi baştan sona yürütür: gönder → sorgula → indir. Gönderim başarılı olduktan sonra iş ücretlenmiş sayılır: durum sorgusundaki
    /// geçici hatalar tek GET olarak yeniden denenir; sonuç yine alınamazsa geçici hata yeniden denenmez koda
    /// (<see cref="ProviderErrorCodes.JobResultLost"/>) çevrilir ve gönderilen görsel sayısı ücretlenmiş kullanım olarak eklenir
    /// (<see cref="ProviderBilledUsage"/>).
    /// </summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    protected async Task<Result<ImageModelOutput>> RunJobAsync(ImageRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var submitted = await SubmitAsync(request, ct);
        if (submitted.IsFailure)
        {
            return submitted.Error;
        }

        var result = await PollAndDownloadAsync(submitted.Value, ct);
        if (result.IsSuccess || result.Error.Code == ProviderErrorCodes.Refused)
        {
            return result;
        }

        var lost = ProviderErrorCodes.IsTransient(result.Error.Code) ? result.Error with { Code = ProviderErrorCodes.JobResultLost } : result.Error;
        return ProviderBilledUsage.Attach(lost, new CallUsage { Images = Math.Max(1, request.Count) });
    }

    private async Task<Result<ImageModelOutput>> PollAndDownloadAsync(Uri statusUrl, CancellationToken ct)
    {
        var started = Stopwatch.GetTimestamp();
        var failures = 0;
        while (true)
        {
            var response = await GuardAsync(
                () =>
                {
                    var poll = new HttpRequestMessage(HttpMethod.Get, statusUrl);
                    ApplyAuth(poll);
                    return SendAsync(poll, ct);
                },
                ct);
            if (response.IsFailure)
            {
                if (!ProviderErrorCodes.IsTransient(response.Error.Code) || ++failures >= MaxConsecutivePollFailures)
                {
                    return response.Error;
                }

                await Task.Delay(Options.PollInterval, ct);
                continue;
            }

            failures = 0;
            var status = ReadJson(response.Value, root => Result.Success(ParseStatus(root)));
            if (status.IsFailure)
            {
                return status.Error;
            }

            if (status.Value.FailureCode is { } code)
            {
                return Error.Failure(code, $"{PlatformDisplayName} görseli üretemedi: {status.Value.FailureMessage}");
            }

            if (status.Value.Completed)
            {
                return await DownloadAllAsync(status.Value.ImageUrls, ct);
            }

            if (Stopwatch.GetElapsedTime(started) > Options.MaxPollDuration)
            {
                return Error.Failure(
                    ProviderErrorCodes.EmptyResponse,
                    $"{PlatformDisplayName} görsel işi {Options.MaxPollDuration.TotalMinutes:0} dakikada tamamlanmadı (zaman aşımı).");
            }

            await Task.Delay(Options.PollInterval, ct);
        }
    }
}
