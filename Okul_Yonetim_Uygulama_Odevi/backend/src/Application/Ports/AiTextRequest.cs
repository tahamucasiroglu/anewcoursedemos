namespace OkulYonetim.Application.Ports;

/// <summary>Metin üretme isteği (spec 0012): sabit talimat (sistem mesajı), içerik (ör. sesli notun dökümü) ve en fazla üretilecek token.</summary>
public sealed record AiTextRequest(string Instruction, string Content, int MaxTokens);
