using System.Text.Json.Nodes;
using TahaKonsept.Agent.Abstractions.Image;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Image.ComfyUi.Abstract;

/// <summary>Model anahtarına göre ComfyUI iş akışı şablonunu bulur ve isteğin değerleriyle doldurur.</summary>
public interface IComfyUiWorkflowProvider
{
    /// <summary>Şablonu olan model anahtarları.</summary>
    IReadOnlyList<string> ModelKeys { get; }

    /// <summary>Şablonu döner.</summary>
    /// <param name="modelKey">Model anahtarı.</param>
    /// <returns>Şablon ya da <c>null</c>.</returns>
    ComfyUiWorkflowTemplate? GetTemplate(string modelKey);

    /// <summary>İsteğe göre doldurulmuş iş akışını üretir (şablon değiştirilmez, kopya döner).</summary>
    /// <param name="request">İstek.</param>
    /// <param name="filenamePrefix">ComfyUI çıktı dosya adı öneki.</param>
    /// <returns>İş akışı ya da hata.</returns>
    Result<JsonObject> Build(ImageRequest request, string filenamePrefix);
}
