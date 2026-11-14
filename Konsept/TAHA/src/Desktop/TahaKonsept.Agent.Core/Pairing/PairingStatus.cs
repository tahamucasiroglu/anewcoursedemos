namespace TahaKonsept.Agent.Core.Pairing;

/// <summary>Eşleme penceresine gösterilecek anlık durum.</summary>
/// <param name="State">Durum.</param>
/// <param name="Message">Türkçe açıklama.</param>
/// <param name="UserCode">Kullanıcıya gösterilecek kod (<c>XXXX-XXXX</c>), bekleme sırasında.</param>
/// <param name="VerificationUrl">Kodun girileceği web adresi.</param>
/// <param name="ExpiresAtUtc">Kodun geçerlilik bitişi.</param>
public sealed record PairingStatus(
    PairingState State,
    string Message,
    string? UserCode = null,
    string? VerificationUrl = null,
    DateTimeOffset? ExpiresAtUtc = null);
