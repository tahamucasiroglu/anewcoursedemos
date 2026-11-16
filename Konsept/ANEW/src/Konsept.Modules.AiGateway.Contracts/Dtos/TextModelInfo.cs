namespace Konsept.Modules.AiGateway.Contracts.Dtos;

/// <summary>Bir metin modeli ve isteği yapan kullanıcı için seçilebilirliği.</summary>
/// <param name="Id">Model kimliği.</param>
/// <param name="Name">Görünen ad.</param>
/// <param name="EffortLevels">Desteklenen efor düzeyleri (boş olabilir).</param>
/// <param name="DefaultEffort">Katalogdaki varsayılan efor; desteklenmiyorsa <c>null</c>.</param>
/// <param name="Selectable">Kullanıcı yetkili ve sağlayıcı key'i geçerliyse <c>true</c>.</param>
public sealed record TextModelInfo(string Id, string Name, IReadOnlyList<string> EffortLevels, string? DefaultEffort, bool Selectable);
