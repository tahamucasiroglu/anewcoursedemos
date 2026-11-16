namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Seçilebilir metin modeli.</summary>
/// <param name="Id">Model kimliği.</param>
/// <param name="Name">Görünen ad.</param>
/// <param name="EffortLevels">Desteklenen efor düzeyleri.</param>
/// <param name="DefaultEffort">Katalog varsayılanı.</param>
public sealed record SelectableModelResponse(string Id, string Name, IReadOnlyList<string> EffortLevels, string? DefaultEffort);
