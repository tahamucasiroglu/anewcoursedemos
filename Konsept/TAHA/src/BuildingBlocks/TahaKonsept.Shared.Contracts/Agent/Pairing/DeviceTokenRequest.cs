namespace TahaKonsept.Shared.Contracts.Agent.Pairing;

/// <summary>
/// Ajanın token yoklama isteği (<c>POST api/agents/device/token</c>). Onay bekleniyorsa API
/// <see cref="DeviceTokenErrorCodes.AuthorizationPending"/> kodlu hata döner; onaylanınca <c>TokenResponse</c>.
/// </summary>
public sealed record DeviceTokenRequest
{
    /// <summary>Başlatma yanıtındaki cihaz kodu.</summary>
    public string DeviceCode { get; init; } = string.Empty;
}
