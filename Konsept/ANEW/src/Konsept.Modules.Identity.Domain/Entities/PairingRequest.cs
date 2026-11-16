using Konsept.Modules.Identity.Domain.Enums;
using Konsept.Modules.Identity.Domain.Rules;

namespace Konsept.Modules.Identity.Domain.Entities;

/// <summary>
/// Eşleştirilmemiş bir ajanın kod isteği (spec 0012 Req 1–5): kod 10 dakika geçerli ve tek kullanımlıktır; ajan durumunu
/// yalnızca kendisine verilen onay sırrıyla (hash'i tutulur) sorar. Onaylanınca ajan oturum belirteci ajana bir kez teslim
/// edilir. Durum geçişleri çağıranın kilidi altında yapılır (bellek deposu, plan Risk 2).
/// </summary>
public sealed class PairingRequest
{
    private PairingRequest(string code, string secretHash, string machineName, string agentVersion, DateTimeOffset createdAt)
    {
        Code = code;
        SecretHash = secretHash;
        MachineName = machineName;
        AgentVersion = agentVersion;
        CreatedAt = createdAt;
        ExpiresAt = createdAt + AgentSessionPolicy.CodeLifetime;
    }

    /// <summary>Gets kod (<c>ABCD-1234</c>).</summary>
    public string Code { get; }

    /// <summary>Gets onay sırrının hash'i.</summary>
    public string SecretHash { get; }

    /// <summary>Gets makine adı.</summary>
    public string MachineName { get; }

    /// <summary>Gets ajan sürümü.</summary>
    public string AgentVersion { get; }

    /// <summary>Gets oluşturma zamanı (UTC).</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>Gets son geçerlilik zamanı (UTC).</summary>
    public DateTimeOffset ExpiresAt { get; }

    /// <summary>Gets durum.</summary>
    public PairingState State { get; private set; }

    /// <summary>Gets onaylayan kullanıcı; onaylanmadıysa <c>null</c>.</summary>
    public Guid? ApprovedBy { get; private set; }

    private string? Token { get; set; }

    /// <summary>Yeni kod isteği oluşturur.</summary>
    /// <param name="code">Kod.</param>
    /// <param name="secretHash">Onay sırrının hash'i.</param>
    /// <param name="machineName">Makine adı.</param>
    /// <param name="agentVersion">Ajan sürümü.</param>
    /// <param name="now">Zaman.</param>
    /// <returns>İstek.</returns>
    public static PairingRequest Open(string code, string secretHash, string machineName, string agentVersion, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretHash);
        return new PairingRequest(code, secretHash, machineName, agentVersion, now.ToUniversalTime());
    }

    /// <summary>Kodun süresi doldu mu.</summary>
    /// <param name="now">Şimdiki zaman.</param>
    /// <returns>Dolduysa <c>true</c>.</returns>
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    /// <summary>Koda şu an karar verilebilir mi.</summary>
    /// <param name="now">Şimdiki zaman.</param>
    /// <returns>Karar.</returns>
    public PairingDecision CanDecide(DateTimeOffset now) =>
        State != PairingState.Pending ? PairingDecision.AlreadyUsed
        : IsExpired(now) ? PairingDecision.Expired
        : PairingDecision.Accepted;

    /// <summary>Onayı başlatır (cihaz kaydedilene kadar belirteç teslim edilmez).</summary>
    /// <param name="userId">Onaylayan kullanıcı.</param>
    /// <param name="now">Zaman.</param>
    /// <returns>Karar.</returns>
    public PairingDecision BeginApproval(Guid userId, DateTimeOffset now)
    {
        var decision = CanDecide(now);
        if (decision == PairingDecision.Accepted)
        {
            State = PairingState.Approving;
            ApprovedBy = userId;
        }

        return decision;
    }

    /// <summary>Cihaz kaydedildi; belirteç ajana teslim edilmeye hazır.</summary>
    /// <param name="token">Ajan oturum belirteci.</param>
    public void CompleteApproval(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        if (State == PairingState.Approving)
        {
            Token = token;
            State = PairingState.Approved;
        }
    }

    /// <summary>Cihaz kaydedilemedi; kod yeniden karar beklemeye döner.</summary>
    public void AbortApproval()
    {
        if (State == PairingState.Approving)
        {
            State = PairingState.Pending;
            ApprovedBy = null;
        }
    }

    /// <summary>Kodu reddeder.</summary>
    /// <param name="now">Zaman.</param>
    /// <returns>Karar.</returns>
    public PairingDecision Reject(DateTimeOffset now)
    {
        var decision = CanDecide(now);
        if (decision == PairingDecision.Accepted)
        {
            State = PairingState.Rejected;
        }

        return decision;
    }

    /// <summary>Onaylanmış isteğin belirtecini bir kez verir; sonra istek tükenir.</summary>
    /// <returns>Belirteç; hazır değilse ya da daha önce verildiyse <c>null</c>.</returns>
    public string? TakeToken()
    {
        if (State != PairingState.Approved)
        {
            return null;
        }

        var token = Token;
        Token = null;
        State = PairingState.Delivered;
        return token;
    }
}
