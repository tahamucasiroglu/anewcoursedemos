namespace Konsept.Web.Models;

/// <summary>"Ajanı eşleştir" formu (spec 0012 Req 3): kullanıcının elle yazdığı kod (adreste taşınmaz).</summary>
/// <param name="Code">Kod.</param>
public sealed record PairingCodeForm(string? Code);
