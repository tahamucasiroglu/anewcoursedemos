namespace Konsept.Modules.AiGateway.Contracts.Dtos;

/// <summary>Metin modeli yanıtı: metin ve çıktı sınırına takılıp yarım kalıp kalmadığı (spec 0013 Req 5; karar çağıranındır).</summary>
/// <param name="Text">Metin (olmayabilir).</param>
/// <param name="Truncated">Yarım kaldıysa <c>true</c>.</param>
public sealed record TextModelReply(string? Text, bool Truncated);
