using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Application.TextModels.Abstract;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Base;

/// <summary>
/// Metin modeli fabrikası tabanı: kaydın sunucuda çalışan bir metin modeli olduğunu ve kimliğin platformla eşleştiğini doğrular;
/// somut sınıf platforma göre nesneyi oluşturur (<see cref="CreateCore"/>; desteklenmeyen platformda null).
/// </summary>
public abstract class TextModelFactoryBase : ITextModelFactory
{
    /// <inheritdoc />
    public Result<ITextModel> Create(ModelInfo model, ProviderCredential credential)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(credential);
        if (model.Capability != ModelCapability.Text || model.RunsOn != ModelRunsOn.Server)
        {
            return Error.Validation(ProviderErrorCodes.ModelNotSupported, $"{model.DisplayName} sunucuda çalışan bir metin modeli değil.");
        }

        if (string.IsNullOrWhiteSpace(model.ProviderModelId))
        {
            return Error.Validation(ProviderErrorCodes.ModelNotSupported, $"{model.DisplayName} için sağlayıcı model kimliği gerekli (ör. openrouter:anthropic/claude-sonnet-5.5).");
        }

        if (!string.Equals(model.Platform, credential.Platform, StringComparison.Ordinal))
        {
            return Error.Validation(ProviderErrorCodes.InvalidRequest, "Kimlik bilgisi model platformuyla eşleşmiyor.");
        }

        var created = CreateCore(model, credential);
        return created is null
            ? Error.Validation(ProviderErrorCodes.ModelNotSupported, $"{model.Platform} platformu için metin modeli desteklenmiyor.")
            : Result.Success(created);
    }

    /// <summary>Platforma göre somut modeli oluşturur.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <returns>Model ya da desteklenmiyorsa null.</returns>
    protected abstract ITextModel? CreateCore(ModelInfo model, ProviderCredential credential);
}
