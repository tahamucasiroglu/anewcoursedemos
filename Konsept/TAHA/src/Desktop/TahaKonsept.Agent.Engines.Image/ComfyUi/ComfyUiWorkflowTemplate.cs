using System.Text.Json.Nodes;

namespace TahaKonsept.Agent.Engines.Image.ComfyUi;

/// <summary>
/// Gömülü ComfyUI iş akışı şablonu (API biçimi). Değerlerde yer tutucular bulunur:
/// <c>{{prompt}}</c>, <c>{{negative}}</c>, <c>{{width}}</c>, <c>{{height}}</c>, <c>{{seed}}</c>, <c>{{steps}}</c>, <c>{{filenamePrefix}}</c>.
/// Değerin tamamı sayısal bir yer tutucuysa sayıya çevrilir.
/// </summary>
/// <param name="ModelKey">Model anahtarı.</param>
/// <param name="DefaultSteps">Varsayılan adım sayısı.</param>
/// <param name="RequiredFiles">ComfyUI <c>models</c> klasöründe bulunması gereken dosyalar (bilgi amaçlı).</param>
/// <param name="Workflow">İş akışı (API biçimi).</param>
public sealed record ComfyUiWorkflowTemplate(string ModelKey, int DefaultSteps, IReadOnlyList<string> RequiredFiles, JsonObject Workflow);
