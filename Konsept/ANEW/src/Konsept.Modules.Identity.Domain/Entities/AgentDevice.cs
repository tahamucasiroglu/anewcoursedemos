using Konsept.Modules.Identity.Domain.Rules;

namespace Konsept.Modules.Identity.Domain.Entities;

/// <summary>
/// Eşleştirilmiş bir ajan (spec 0012 Req 4, 9): kodu onaylayan kullanıcıya bağlıdır; ajan oturum belirtecinin yalnızca SHA-256
/// hash'i tutulur. İptal edilmemiş ve son kullanımdan bu yana 90 gün geçmemişse etkindir. Kullanıcı başına tek etkin ajan
/// vardır (yeni eşleştirme öncekini iptal eder).
/// </summary>
public sealed class AgentDevice
{
    private AgentDevice(Guid id, Guid userId, string machineName, string agentVersion, string tokenHash, DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        MachineName = machineName;
        AgentVersion = agentVersion;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        LastUsedAt = createdAt;
    }

    /// <summary>Gets cihaz kimliği (UUIDv7).</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets sahibi (kodu onaylayan kullanıcı).</summary>
    public Guid UserId { get; private set; }

    /// <summary>Gets makine adı (ajanın bildirdiği).</summary>
    public string MachineName { get; private set; }

    /// <summary>Gets eşleşme anındaki ajan sürümü.</summary>
    public string AgentVersion { get; private set; }

    /// <summary>Gets ajan oturum belirtecinin hash'i.</summary>
    public string TokenHash { get; private set; }

    /// <summary>Gets eşleşme zamanı (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Gets son kullanım zamanı (UTC).</summary>
    public DateTimeOffset LastUsedAt { get; private set; }

    /// <summary>Gets iptal zamanı; iptal edilmemişse <c>null</c>.</summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Yeni eşleşmiş cihaz oluşturur.</summary>
    /// <param name="userId">Sahip.</param>
    /// <param name="machineName">Makine adı.</param>
    /// <param name="agentVersion">Ajan sürümü.</param>
    /// <param name="tokenHash">Belirtecin hash'i.</param>
    /// <param name="now">Zaman.</param>
    /// <returns>Cihaz.</returns>
    public static AgentDevice Pair(Guid userId, string machineName, string agentVersion, string tokenHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(machineName);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        var utc = now.ToUniversalTime();
        return new AgentDevice(Guid.CreateVersion7(utc), userId, machineName, agentVersion, tokenHash, utc);
    }

    /// <summary>Ajan oturumu şu an geçerli mi.</summary>
    /// <param name="now">Şimdiki zaman.</param>
    /// <returns>Geçerliyse <c>true</c>.</returns>
    public bool IsActive(DateTimeOffset now) => RevokedAt is null && !AgentSessionPolicy.IsExpired(LastUsedAt, now);

    /// <summary>Son kullanımı günceller (bellek içi uygulamalar için).</summary>
    /// <param name="now">Kullanım zamanı.</param>
    public void Touch(DateTimeOffset now) => LastUsedAt = now.ToUniversalTime();

    /// <summary>Cihazı iptal eder (zaten iptalse değişmez).</summary>
    /// <param name="now">İptal zamanı.</param>
    public void Revoke(DateTimeOffset now) => RevokedAt ??= now.ToUniversalTime();
}
