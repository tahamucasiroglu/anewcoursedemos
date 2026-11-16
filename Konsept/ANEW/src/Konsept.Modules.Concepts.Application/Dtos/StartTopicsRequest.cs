using System.Text.Json;

namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>Konu üret isteği; adet ham JSON değeridir (yalnızca 1–100 arası tam sayı kabul edilir, AC-2).</summary>
/// <param name="ProfileId">Profil.</param>
/// <param name="Count">Adet.</param>
public sealed record StartTopicsRequest(Guid? ProfileId, JsonElement? Count);
