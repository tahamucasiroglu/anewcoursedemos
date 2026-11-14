using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.ImageModels.Abstract;
using TahaKonsept.Modules.Providers.Application.Media.Abstract;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Application.SpeechModels.Abstract;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Media.Base;

/// <summary>
/// Görsel/ses modeli fabrikası tabanı: kaydın istenen yetenekte online model olduğunu, sağlayıcı kimliğinin dolu olduğunu ve
/// kimlik bilgisinin platformla eşleştiğini doğrular (tek yerde); somut sınıf platforma göre nesneyi oluşturur
/// (<see cref="CreateImageCore"/>, <see cref="CreateSpeechCore"/>; desteklenmeyen platformda null).
/// </summary>
public abstract class MediaModelFactoryBase : IMediaModelFactory
{
    /// <inheritdoc />
    public Result<IImageModel> CreateImage(ModelInfo model, ProviderCredential credential) =>
        Create(model, credential, ModelCapability.Image, "görsel", CreateImageCore);

    /// <inheritdoc />
    public Result<ISpeechModel> CreateSpeech(ModelInfo model, ProviderCredential credential) =>
        Create(model, credential, ModelCapability.Speech, "ses", CreateSpeechCore);

    /// <summary>Platforma göre somut görsel modelini oluşturur.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <returns>Model ya da desteklenmiyorsa null.</returns>
    protected abstract IImageModel? CreateImageCore(ModelInfo model, ProviderCredential credential);

    /// <summary>Platforma göre somut ses modelini oluşturur.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <returns>Model ya da desteklenmiyorsa null.</returns>
    protected abstract ISpeechModel? CreateSpeechCore(ModelInfo model, ProviderCredential credential);

    /// <summary>Ortak doğrulama + oluşturma.</summary>
    /// <typeparam name="TModel">Model arayüzü.</typeparam>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="capability">Beklenen yetenek.</param>
    /// <param name="label">Hata mesajı için yetenek adı.</param>
    /// <param name="create">Somut oluşturucu.</param>
    private static Result<TModel> Create<TModel>(
        ModelInfo model,
        ProviderCredential credential,
        ModelCapability capability,
        string label,
        Func<ModelInfo, ProviderCredential, TModel?> create)
        where TModel : class
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(credential);
        if (model.Capability != capability || model.RunsOn != ModelRunsOn.Server)
        {
            return Error.Validation(ProviderErrorCodes.ModelNotSupported, $"{model.DisplayName} online bir {label} modeli değil.");
        }

        if (string.IsNullOrWhiteSpace(model.ProviderModelId))
        {
            return Error.Validation(ProviderErrorCodes.ModelNotSupported, $"{model.DisplayName} için sağlayıcı model kimliği tanımlı değil.");
        }

        if (!string.Equals(model.Platform, credential.Platform, StringComparison.Ordinal))
        {
            return Error.Validation(ProviderErrorCodes.InvalidRequest, "Kimlik bilgisi model platformuyla eşleşmiyor.");
        }

        var created = create(model, credential);
        return created is null
            ? Error.Validation(ProviderErrorCodes.ModelNotSupported, $"{model.Platform} platformu için {label} modeli desteklenmiyor.")
            : Result.Success(created);
    }
}
