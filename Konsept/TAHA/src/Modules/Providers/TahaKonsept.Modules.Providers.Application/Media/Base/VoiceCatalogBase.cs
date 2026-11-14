using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Media.Abstract;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Media.Base;

/// <summary>
/// Ses kataloğu tabanı: anahtarı doğrular, kaydın online ses modeli olduğunu denetler, somut sınıfın verdiği listeyi
/// sıralar (Türkçeye özgü → varsayılan → diğerleri; aynı grupta orijinal sıra korunur). Somut sınıf <see cref="VoicesFor"/>'u yazar.
/// </summary>
public abstract class VoiceCatalogBase : IVoiceCatalog
{
    /// <summary>Kurucu.</summary>
    /// <param name="catalog">Model kataloğu.</param>
    protected VoiceCatalogBase(IModelCatalog catalog)
    {
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    /// <summary>Model kataloğu.</summary>
    protected IModelCatalog Catalog { get; }

    /// <inheritdoc />
    public Result<IReadOnlyList<VoiceInfo>> List(string? modelKey)
    {
        if (string.IsNullOrWhiteSpace(modelKey))
        {
            return Error.Validation(ProviderErrorCodes.InvalidRequest, "Model anahtarı gerekli (?modelKey=).");
        }

        var model = Catalog.Find(modelKey.Trim());
        if (model is null)
        {
            return Error.NotFound(ProviderErrorCodes.ModelNotFound, $"Model katalogda yok: {modelKey}");
        }

        if (model.Capability != ModelCapability.Speech || model.RunsOn != ModelRunsOn.Server)
        {
            return Error.Validation(ProviderErrorCodes.ModelNotSupported, $"{model.DisplayName} bir online ses modeli değil.");
        }

        return Result.Success(Ordered(VoicesFor(model)));
    }

    /// <inheritdoc />
    public string? DefaultVoiceId(string modelKey)
    {
        var model = string.IsNullOrWhiteSpace(modelKey) ? null : Catalog.Find(modelKey.Trim());
        if (model is null)
        {
            return null;
        }

        var voices = VoicesFor(model);
        return (voices.FirstOrDefault(v => v.IsDefault) ?? voices.FirstOrDefault())?.Id;
    }

    /// <summary>Modelin sesleri (sırasız; boş liste = ses seçimi yok).</summary>
    /// <param name="model">Online ses modeli kaydı.</param>
    protected abstract IReadOnlyList<VoiceInfo> VoicesFor(ModelInfo model);

    /// <summary>Türkçeye özgü sesler önce, sonra varsayılan, sonra diğerleri.</summary>
    /// <param name="voices">Sesler.</param>
    private static IReadOnlyList<VoiceInfo> Ordered(IReadOnlyList<VoiceInfo> voices) =>
        voices.OrderByDescending(v => v.NativeTurkish).ThenByDescending(v => v.IsDefault).ToList();
}
