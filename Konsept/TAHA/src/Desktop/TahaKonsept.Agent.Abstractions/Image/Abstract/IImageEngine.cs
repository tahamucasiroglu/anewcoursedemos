using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Agent.Abstractions.Engines.Abstract;
using TahaKonsept.Agent.Abstractions.Image;

namespace TahaKonsept.Agent.Abstractions.Image.Abstract;

/// <summary>Yerel görsel üretim motoru (stable-diffusion.cpp, ComfyUI). Bir motor birden çok modeli çalıştırabilir.</summary>
public interface IImageEngine : IAgentEngine
{
    /// <summary>Bu motorun çalıştırabildiği model anahtarları.</summary>
    IReadOnlyList<string> SupportedModels { get; }

    /// <summary>Görsel üretir.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görsel dosyası.</returns>
    Task<Result<ImageOutput>> GenerateAsync(ImageRequest request, CancellationToken ct);
}
