namespace Konsept.Modules.Profiles.Application.Dtos;

/// <summary>İnce ayar seçimi (her grupta tek değer; <c>null</c> = Belirtilmedi).</summary>
/// <param name="Tone">Ton.</param>
/// <param name="Audience">İzleyici.</param>
/// <param name="Narration">Anlatım biçimi.</param>
public sealed record FineTuneRequest(string? Tone, string? Audience, string? Narration);
