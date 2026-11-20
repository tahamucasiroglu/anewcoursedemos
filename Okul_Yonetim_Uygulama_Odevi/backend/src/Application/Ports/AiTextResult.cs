namespace OkulYonetim.Application.Ports;

/// <summary>Modelin ürettiği metin (spec 0012; boş olmaz — boş yanıt "kullanılamıyor" sayılır).</summary>
public sealed record AiTextResult(string Text);
