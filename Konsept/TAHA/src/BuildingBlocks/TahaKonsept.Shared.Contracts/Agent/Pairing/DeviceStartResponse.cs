namespace TahaKonsept.Shared.Contracts.Agent.Pairing;

/// <summary>
/// Cihaz eşleme başlatma yanıtı (RFC 8628 benzeri): ajan <see cref="UserCode"/>'u gösterir, kullanıcı web'de onaylar,
/// ajan <see cref="DeviceCode"/> ile <see cref="IntervalSeconds"/> aralıkla <c>POST api/agents/device/token</c> yoklar.
/// </summary>
public sealed record DeviceStartResponse
{
    /// <summary>Gizli cihaz kodu (yalnızca ajanda kalır).</summary>
    public string DeviceCode { get; init; } = string.Empty;

    /// <summary>Kullanıcıya gösterilen 8 haneli kod (<c>XXXX-XXXX</c>).</summary>
    public string UserCode { get; init; } = string.Empty;

    /// <summary>Kullanıcının kodu onaylayacağı web adresi.</summary>
    public string VerificationUrl { get; init; } = string.Empty;

    /// <summary>Kodu önceden doldurulmuş onay adresi (ajan tarayıcıyı doğrudan bununla açabilir).</summary>
    public string VerificationUrlComplete { get; init; } = string.Empty;

    /// <summary>Yoklama aralığı (saniye).</summary>
    public int IntervalSeconds { get; init; } = 5;

    /// <summary>Kodun geçerlilik süresi (saniye).</summary>
    public int ExpiresInSeconds { get; init; } = 600;
}
