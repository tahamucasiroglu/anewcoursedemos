namespace TahaKonsept.Shared.Contracts.Agent.Pairing;

/// <summary>
/// Kullanıcının web'de masaüstü programı kodunu onaylama isteği (<c>POST api/agents/device/approve</c>, <c>deny</c>).
/// </summary>
public sealed record DeviceApproveRequest
{
    /// <summary>Ajanın gösterdiği kod (<c>XXXX-XXXX</c>; tire ve büyük/küçük harf önemsiz).</summary>
    public string UserCode { get; init; } = string.Empty;
}
