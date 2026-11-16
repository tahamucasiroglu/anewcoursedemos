using System.Text.Json;

namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>Elle düzenlenmiş sahne (spec 0015 Req 3). Süre ham JSON'dur: yalnızca pozitif tam sayı kabul edilir (ondalık, metin reddedilir).</summary>
/// <param name="Title">Sahne başlığı.</param>
/// <param name="Visual">Ekran tarifi.</param>
/// <param name="Narration">Seslendirme metni.</param>
/// <param name="DurationSeconds">Süre (saniye).</param>
public sealed record SceneInput(string? Title, string? Visual, string? Narration, JsonElement? DurationSeconds);
