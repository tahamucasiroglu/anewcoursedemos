using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.AiModels.Base;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.ImageModels.Abstract;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.ImageModels.Base;

/// <summary>
/// Online görsel modellerinin ortak tabanı. Somut sınıf yalnızca <see cref="GenerateCoreAsync"/>'i yazar (tek HTTP çağrısı);
/// doğrulama, yeniden deneme ve CallLog/maliyet burada. Somut sınıflar <c>Infrastructure/ImageModels/Concrete</c> altına eklenir.
/// </summary>
public abstract class ImageModelBase : AiModelBase, IImageModel
{
    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    protected ImageModelBase(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies)
        : base(model, credential, dependencies)
    {
    }

    /// <inheritdoc />
    public async Task<Result<ImageResult>> GenerateAsync(ImageRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Prompt) || request.Width <= 0 || request.Height <= 0 || request.Count <= 0)
        {
            return Error.Validation(ProviderErrorCodes.InvalidRequest, "Görsel isteği geçersiz (prompt, boyut veya adet).");
        }

        var call = await ExecuteMeteredAsync(request.Context, c => GenerateCoreAsync(request, c), o => o.Usage, ct);
        return call.Map(m => new ImageResult(m.Value.Images, m.Value.MimeType, Model.Key, m.CostUsd, m.DurationMs));
    }

    /// <summary>Tek HTTP çağrısı ile görsel üretir.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    protected abstract Task<Result<ImageModelOutput>> GenerateCoreAsync(ImageRequest request, CancellationToken ct);
}
