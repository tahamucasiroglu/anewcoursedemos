namespace Konsept.Modules.Concepts.Domain.Entities;

/// <summary>Profilin ince ayarı (spec 0005): ton, izleyici, anlatım biçimi kodları; <c>null</c> = "Belirtilmedi" (talimata girmez).</summary>
/// <param name="Tone">Ton kodu.</param>
/// <param name="Audience">İzleyici kodu.</param>
/// <param name="Narration">Anlatım biçimi kodu.</param>
public sealed record FineTuneChoice(string? Tone, string? Audience, string? Narration);
