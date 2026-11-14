using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.AiModels.Abstract;
using TahaKonsept.Modules.Providers.Application.Models;

namespace TahaKonsept.Modules.Providers.Application.ImageModels.Abstract;

/// <summary>
/// Online görsel modeli (Nano Banana, gpt-image, fal, BFL, GLM-Image, Replicate…). Somut sınıflar ayrı bir adımda
/// <c>Infrastructure/ImageModels/Concrete</c> altına eklenecek; ortak davranış <c>ImageModelBase</c>'de.
/// </summary>
public interface IImageModel : IAiModel
{
    /// <summary>Görsel üretir.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<Result<ImageResult>> GenerateAsync(ImageRequest request, CancellationToken ct);
}
