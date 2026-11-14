using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Base;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Media.Abstract;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Concrete;

/// <summary>
/// <see cref="IMediaGateway"/> uygulaması (tekil; arka plan servislerinden güvenle çağrılır): kullanıcıyı doğrular, katalogdan
/// online görsel/ses modelini bulur, anahtarı <see cref="IApiKeyVault"/> ile çözer, <see cref="IMediaModelFactory"/> ile somut
/// modeli kurar ve çalıştırır. Ses isteğinde ses kimliği yoksa <see cref="IVoiceCatalog"/> varsayılanı kullanılır. Yeniden deneme
/// ve CallLog/maliyet (görsel başı, bin karakter, dakika, token) model tabanında (<c>AiModelBase</c>).
/// </summary>
public sealed class MediaGateway : ProvidersServiceBase, IMediaGateway
{
    private readonly IModelCatalog _catalog;
    private readonly IApiKeyVault _vault;
    private readonly IMediaModelFactory _factory;
    private readonly IVoiceCatalog _voices;
    private readonly ILogger<MediaGateway> _logger;

    /// <summary>Kurucu.</summary>
    /// <param name="catalog">Model kataloğu.</param>
    /// <param name="vault">Anahtar kasası.</param>
    /// <param name="factory">Görsel/ses modeli fabrikası.</param>
    /// <param name="voices">Ses kataloğu.</param>
    /// <param name="clock">Saat.</param>
    /// <param name="logger">Logger.</param>
    public MediaGateway(
        IModelCatalog catalog,
        IApiKeyVault vault,
        IMediaModelFactory factory,
        IVoiceCatalog voices,
        IClock clock,
        ILogger<MediaGateway> logger)
        : base(clock)
    {
        _catalog = catalog;
        _vault = vault;
        _factory = factory;
        _voices = voices;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<ImageResult>> GenerateImageAsync(string modelKey, ImageRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var model = await ResolveAsync(modelKey, request.Context, ModelCapability.Image, "görsel", _factory.CreateImage, ct);
        if (model.IsFailure)
        {
            return model.Error;
        }

        var result = await model.Value.GenerateAsync(request, ct);
        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Görsel üretildi: {Model}, {Count} adet, ${Cost} ({Purpose}).",
                modelKey, result.Value.Images.Count, result.Value.CostUsd, request.Context.Purpose);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<SpeechResult>> SynthesizeSpeechAsync(string modelKey, SpeechRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var model = await ResolveAsync(modelKey, request.Context, ModelCapability.Speech, "ses", _factory.CreateSpeech, ct);
        if (model.IsFailure)
        {
            return model.Error;
        }

        var effective = string.IsNullOrWhiteSpace(request.Voice)
            ? request with { Voice = _voices.DefaultVoiceId(model.Value.ModelKey) }
            : request;
        var result = await model.Value.SynthesizeAsync(effective, ct);
        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Ses üretildi: {Model}, {Chars} karakter, {Seconds} sn, ${Cost} ({Purpose}).",
                modelKey, effective.Text.Length, result.Value.AudioSeconds, result.Value.CostUsd, effective.Context.Purpose);
        }

        return result;
    }

    /// <summary>Kullanıcı + katalog + anahtar doğrulaması ve somut modelin kurulması (görsel/ses ortak).</summary>
    /// <typeparam name="TModel">Model arayüzü.</typeparam>
    /// <param name="modelKey">Katalog anahtarı.</param>
    /// <param name="context">Kullanıcı + amaç.</param>
    /// <param name="capability">Beklenen yetenek.</param>
    /// <param name="label">Hata mesajı için yetenek adı.</param>
    /// <param name="create">Fabrika metodu.</param>
    /// <param name="ct">İptal belirteci.</param>
    private async Task<Result<TModel>> ResolveAsync<TModel>(
        string modelKey,
        CallContext context,
        ModelCapability capability,
        string label,
        Func<ModelInfo, ProviderCredential, Result<TModel>> create,
        CancellationToken ct)
    {
        if (context is null || context.UserId == Guid.Empty)
        {
            return Error.Validation(ProviderErrorCodes.InvalidRequest, "Kullanıcı kimliği gerekli.");
        }

        var model = string.IsNullOrWhiteSpace(modelKey) ? null : _catalog.Find(modelKey.Trim());
        if (model is null)
        {
            return Error.NotFound(ProviderErrorCodes.ModelNotFound, $"Model katalogda yok: {modelKey}");
        }

        if (model.Capability != capability || model.RunsOn != ModelRunsOn.Server)
        {
            return Error.Validation(ProviderErrorCodes.ModelNotSupported, $"{model.DisplayName} bir online {label} modeli değil.");
        }

        var credential = await _vault.GetCredentialAsync(context.UserId, model.Platform, ct);
        return credential.IsFailure ? credential.Error : create(model, credential.Value);
    }
}
