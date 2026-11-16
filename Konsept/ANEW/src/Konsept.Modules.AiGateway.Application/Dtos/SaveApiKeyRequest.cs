namespace Konsept.Modules.AiGateway.Application.Dtos;

/// <summary>API key kaydetme/değiştirme isteği (spec 0004 Req 7–8).</summary>
/// <param name="ApiKey">Girilen key (düz metin yalnızca istekte; şifrelenip saklanır, hiçbir yere yazılmaz).</param>
public sealed record SaveApiKeyRequest(string? ApiKey);
