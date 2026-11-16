namespace Konsept.Modules.AiGateway.Contracts.Dtos;

/// <summary>
/// Metin modeli yanıtı: metin, çıktı sınırına takılıp yarım kalıp kalmadığı (spec 0013 Req 5; karar çağıranındır) ve kapının bu çağrı için
/// kaydettiği maliyet (USD; spec 0017 Req 16 iş toplamı).
/// </summary>
/// <param name="Text">Metin (olmayabilir).</param>
/// <param name="Truncated">Yarım kaldıysa <c>true</c>.</param>
/// <param name="CostUsd">Kaydedilen maliyet (USD).</param>
public sealed record TextModelReply(string? Text, bool Truncated, decimal CostUsd = 0m);
