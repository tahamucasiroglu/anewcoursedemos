namespace Konsept.Modules.Profiles.Application.Dtos;

/// <summary>Modalda seçilebilen metin modeli (yetkili + geçerli key, spec 0005 Req 5).</summary>
/// <param name="Id">Model kimliği.</param>
/// <param name="Name">Görünen ad.</param>
/// <param name="EffortLevels">Desteklenen efor düzeyleri.</param>
/// <param name="DefaultEffort">Katalog varsayılanı.</param>
public sealed record SelectableModelDto(string Id, string Name, IReadOnlyList<string> EffortLevels, string? DefaultEffort);
