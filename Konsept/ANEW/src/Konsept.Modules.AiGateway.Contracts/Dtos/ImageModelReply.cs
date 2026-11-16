namespace Konsept.Modules.AiGateway.Contracts.Dtos;

/// <summary>Online görsel yanıtı: görsel (base64; yoksa <c>null</c>) ve kaydedilen maliyet (USD).</summary>
/// <param name="ImageBase64">Görsel.</param>
/// <param name="CostUsd">Maliyet (USD).</param>
public sealed record ImageModelReply(string? ImageBase64, decimal CostUsd);
