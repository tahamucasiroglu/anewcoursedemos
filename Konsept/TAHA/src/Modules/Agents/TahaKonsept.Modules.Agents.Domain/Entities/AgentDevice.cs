using TahaKonsept.BuildingBlocks.Domain.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Base;

namespace TahaKonsept.Modules.Agents.Domain.Entities;

/// <summary>
/// Eşlenmiş masaüstü programı (ajan). Eşlemede <see cref="Pair"/> ile oluşur; hub'a bağlanınca <see cref="ApplyHello"/> ile
/// makine/ağ/yetenek bilgisi, <see cref="Touch"/> ile son görülme güncellenir. Akış anahtarı <see cref="StreamKeyProtected"/>
/// içinde şifreli (<c>ISecretProtector</c>) durur. <see cref="Revoke"/> eşlemeyi iptal eder (liste ve hub'dan düşer).
/// </summary>
public sealed class AgentDevice : AggregateRoot, IAuditable, IHasUser
{
    private AgentDevice()
    {
    }

    /// <summary>Sahip kullanıcı.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Bilgisayar adı.</summary>
    public string MachineName { get; private set; } = string.Empty;

    /// <summary>Ajan sürümü.</summary>
    public string AgentVersion { get; private set; } = string.Empty;

    /// <summary>İşletim sistemi açıklaması.</summary>
    public string? OsDescription { get; private set; }

    /// <summary>Eşleme anı (UTC).</summary>
    public DateTimeOffset PairedAtUtc { get; private set; }

    /// <summary>Son görülme (UTC).</summary>
    public DateTimeOffset? LastSeenAtUtc { get; private set; }

    /// <summary>Yerel ağ adresi.</summary>
    public string? LanAddress { get; private set; }

    /// <summary>Yerel video sunucusu portu.</summary>
    public int? VideoServerPort { get; private set; }

    /// <summary>Şifreli akış anahtarı.</summary>
    public string? StreamKeyProtected { get; private set; }

    /// <summary>Son bildirilen yetenekler (JSON, <c>AgentCapabilities</c>).</summary>
    public string? CapabilitiesJson { get; private set; }

    /// <summary>Ajana verilen refresh token ailesi (Identity; eşleme kaldırılınca iptal edilir).</summary>
    public Guid? TokenFamilyId { get; private set; }

    /// <summary>Çevrimiçi mi (son bilinen durum; canlı durum bağlantı izleyicisindedir).</summary>
    public bool IsOnline { get; private set; }

    /// <summary>Eşleme iptal edildi mi.</summary>
    public bool IsRevoked { get; private set; }

    /// <summary>İptal anı (UTC).</summary>
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    /// <summary>Oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Güncellenme anı (UTC).</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Yeni eşlenmiş cihaz oluşturur.</summary>
    /// <param name="userId">Sahip.</param>
    /// <param name="machineName">Bilgisayar adı.</param>
    /// <param name="agentVersion">Sürüm.</param>
    /// <param name="now">Şu an.</param>
    /// <returns>Cihaz.</returns>
    public static AgentDevice Pair(Guid userId, string machineName, string agentVersion, DateTimeOffset now)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Kullanıcı kimliği boş olamaz.", nameof(userId));
        }

        return new AgentDevice
        {
            UserId = userId,
            MachineName = string.IsNullOrWhiteSpace(machineName) ? "Bilgisayar" : machineName.Trim(),
            AgentVersion = agentVersion?.Trim() ?? string.Empty,
            PairedAtUtc = now,
        };
    }

    /// <summary>Aynı bilgisayar yeniden eşlenince sürüm ve eşleme zamanını yeniler.</summary>
    /// <param name="agentVersion">Sürüm.</param>
    /// <param name="now">Şu an.</param>
    public void Repair(string agentVersion, DateTimeOffset now)
    {
        if (!string.IsNullOrWhiteSpace(agentVersion))
        {
            AgentVersion = agentVersion.Trim();
        }

        PairedAtUtc = now;
    }

    /// <summary>Yeni verilen token ailesini bağlar; öncekini döner (iptal edilmesi için).</summary>
    /// <param name="tokenFamilyId">Yeni aile.</param>
    /// <returns>Önceki aile.</returns>
    public Guid? AssignTokenFamily(Guid? tokenFamilyId)
    {
        var previous = TokenFamilyId;
        TokenFamilyId = tokenFamilyId;
        return previous;
    }

    /// <summary>Hub tanıtımını uygular ve cihazı çevrimiçi işaretler.</summary>
    /// <param name="machineName">Bilgisayar adı.</param>
    /// <param name="agentVersion">Sürüm.</param>
    /// <param name="osDescription">İşletim sistemi.</param>
    /// <param name="lanAddress">LAN adresi.</param>
    /// <param name="videoServerPort">Video sunucusu portu.</param>
    /// <param name="streamKeyProtected">Şifreli akış anahtarı.</param>
    /// <param name="capabilitiesJson">Yetenek JSON'u.</param>
    /// <param name="now">Şu an.</param>
    public void ApplyHello(
        string machineName,
        string agentVersion,
        string? osDescription,
        string? lanAddress,
        int? videoServerPort,
        string? streamKeyProtected,
        string? capabilitiesJson,
        DateTimeOffset now)
    {
        if (!string.IsNullOrWhiteSpace(machineName))
        {
            MachineName = machineName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(agentVersion))
        {
            AgentVersion = agentVersion.Trim();
        }

        OsDescription = osDescription;
        LanAddress = lanAddress;
        VideoServerPort = videoServerPort;
        StreamKeyProtected = streamKeyProtected;
        CapabilitiesJson = capabilitiesJson;
        MarkOnline(now);
    }

    /// <summary>Son görülmeyi günceller.</summary>
    /// <param name="now">Şu an.</param>
    public void Touch(DateTimeOffset now) => LastSeenAtUtc = now;

    /// <summary>Çevrimiçi işaretler.</summary>
    /// <param name="now">Şu an.</param>
    public void MarkOnline(DateTimeOffset now)
    {
        IsOnline = true;
        LastSeenAtUtc = now;
    }

    /// <summary>Çevrimdışı işaretler.</summary>
    /// <param name="now">Şu an.</param>
    public void MarkOffline(DateTimeOffset now)
    {
        IsOnline = false;
        LastSeenAtUtc = now;
    }

    /// <summary>Eşlemeyi iptal eder (tekrar çağrı etkisizdir).</summary>
    /// <param name="now">Şu an.</param>
    public void Revoke(DateTimeOffset now)
    {
        if (IsRevoked)
        {
            return;
        }

        IsRevoked = true;
        IsOnline = false;
        RevokedAtUtc = now;
    }
}
