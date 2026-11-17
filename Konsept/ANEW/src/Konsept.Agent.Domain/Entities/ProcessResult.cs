namespace Konsept.Agent.Domain.Entities;

/// <summary>Dış sürecin sonucu (spec 0028): çıkış kodu, çıktının sonu (standart çıktı + hata; teşhis için) ve süre sınırına takıldı mı.</summary>
/// <param name="ExitCode">Çıkış kodu (başlatılamadıysa -1).</param>
/// <param name="Output">Çıktının son kısmı.</param>
/// <param name="TimedOut">Süre sınırı aşıldı mı.</param>
public sealed record ProcessResult(int ExitCode, string Output, bool TimedOut = false)
{
    /// <summary>Gets a value indicating whether süreç başarıyla bitti.</summary>
    public bool Ok => ExitCode == 0 && !TimedOut;
}
