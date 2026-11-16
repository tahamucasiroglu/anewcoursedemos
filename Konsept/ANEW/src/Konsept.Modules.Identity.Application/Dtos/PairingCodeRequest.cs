namespace Konsept.Modules.Identity.Application.Dtos;

/// <summary>
/// Web'in eşleştirme kodu isteği (sorgu/onay/ret). Kod yalnızca gövdede taşınır; adreste olsaydı istek loguna düz yazılırdı
/// (review r1 M-2).
/// </summary>
/// <param name="Code">Kullanıcının yazdığı kod.</param>
public sealed record PairingCodeRequest(string? Code);
