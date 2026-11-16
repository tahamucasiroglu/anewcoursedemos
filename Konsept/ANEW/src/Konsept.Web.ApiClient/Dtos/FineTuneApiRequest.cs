namespace Konsept.Web.ApiClient.Dtos;

/// <summary>İnce ayar seçimi (<c>null</c> = Belirtilmedi).</summary>
/// <param name="Tone">Ton.</param>
/// <param name="Audience">İzleyici.</param>
/// <param name="Narration">Anlatım biçimi.</param>
public sealed record FineTuneApiRequest(string? Tone, string? Audience, string? Narration);
