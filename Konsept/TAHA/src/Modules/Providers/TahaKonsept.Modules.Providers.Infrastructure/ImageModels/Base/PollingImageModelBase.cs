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

    /// <summary>Tek işi baştan sona yürütür: gönder → sorgula → indir.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    protected async Task<Result<ImageModelOutput>> RunJobAsync(ImageRequest request, CancellationToken ct)
    {
        var submitted = await SubmitAsync(request, ct);
        if (submitted.IsFailure)
        {
            return submitted.Error;
        }

        var started = Stopwatch.GetTimestamp();
        while (true)
        {
            var poll = new HttpRequestMessage(HttpMethod.Get, submitted.Value);
            ApplyAuth(poll);
            var response = await SendAsync(poll, ct);
            if (response.IsFailure)
            {
                return response.Error;
            }

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
