namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>Kayıt cevabı (spec 0023 Req 4): sunucunun ürün versiyonu ve ajanın güncellenmesi gerekip gerekmediği.</summary>
/// <param name="ServerVersion">Sunucu sürümü.</param>
/// <param name="UpdateRequired">Ajan sürümü sunucudan farklı; ajan komut almaz, güncelleme ekranını açar.</param>
public sealed record AgentRegisterReply(string ServerVersion, bool UpdateRequired);
