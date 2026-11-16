namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>Doğrudan düzenleme (spec 0015 Req 3): konseptin tüm sahneleri, aynı sayıda ve sırada.</summary>
/// <param name="Scenes">Sahneler.</param>
public sealed record SaveScenesRequest(IReadOnlyList<SceneInput>? Scenes);
