using System.Text.Json;

namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>Fikirden üret isteği (fikir en fazla 2.000 karakter, adet 1–100; G-5).</summary>
/// <param name="ProfileId">Profil.</param>
/// <param name="Idea">Fikir.</param>
/// <param name="Count">Adet (ham JSON).</param>
public sealed record StartFromIdeaRequest(Guid? ProfileId, string? Idea, JsonElement? Count);
