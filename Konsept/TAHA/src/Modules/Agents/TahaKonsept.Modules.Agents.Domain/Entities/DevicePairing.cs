using TahaKonsept.BuildingBlocks.Domain.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Domain.Enums;
using TahaKonsept.Modules.Agents.Domain.Errors;

namespace TahaKonsept.Modules.Agents.Domain.Entities;

/// <summary>
/// RFC 8628 benzeri cihaz eşleme isteği ve durum makinesi. Ajan <see cref="Start"/> ile açar (gizli cihaz kodunun yalnızca hash'i saklanır),
/// kullanıcı web'de <see cref="Approve"/>/<see cref="Deny"/> eder, ajan <see cref="Poll"/> ile yoklar; onaylıysa token bir kez verilir ve
/// <see cref="Consume"/> ile kapanır. Süre: <see cref="ExpiresAtUtc"/>; yoklama aralığı: <see cref="IntervalSeconds"/> (çok sık yoklamada +5 sn).
/// <see cref="UserId"/> onaya kadar <see cref="Guid.Empty"/>'dir.
/// </summary>
public sealed class DevicePairing : AggregateRoot, IAuditable, IHasUser
{
    /// <summary>Yavaşla yanıtında aralığa eklenen saniye (RFC 8628 §3.5).</summary>
    public const int SlowDownIncrementSeconds = 5;

    private DevicePairing()
    {
    }

    /// <summary>Onaylayan kullanıcı (onaya kadar boş).</summary>
    public Guid UserId { get; private set; }

    /// <summary>Cihaz kodunun SHA256 hash'i (hex).</summary>
    public string DeviceCodeHash { get; private set; } = string.Empty;

    /// <summary>Kullanıcı kodu (tiresiz, büyük harf, 8 karakter).</summary>
    public string UserCode { get; private set; } = string.Empty;

    /// <summary>Bilgisayar adı.</summary>
    public string MachineName { get; private set; } = string.Empty;

    /// <summary>Ajan sürümü.</summary>
    public string AgentVersion { get; private set; } = string.Empty;

    /// <summary>Durum.</summary>
    public DevicePairingStatus Status { get; private set; }

    /// <summary>Bitiş anı (UTC).</summary>
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    /// <summary>Geçerli yoklama aralığı (saniye).</summary>
    public int IntervalSeconds { get; private set; }

    /// <summary>Son yoklama anı (UTC).</summary>
    public DateTimeOffset? LastPolledAtUtc { get; private set; }

    /// <summary>Onay/ret anı (UTC).</summary>
    public DateTimeOffset? DecidedAtUtc { get; private set; }

    /// <summary>Token verilme anı (UTC).</summary>
    public DateTimeOffset? ConsumedAtUtc { get; private set; }

    /// <summary>Token verilince bağlanan cihaz kimliği.</summary>
    public Guid? AgentDeviceId { get; private set; }

    /// <summary>Oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Güncellenme anı (UTC).</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Yeni eşleme isteği açar.</summary>
    /// <param name="deviceCodeHash">Cihaz kodu hash'i.</param>
    /// <param name="userCode">Kullanıcı kodu (normalize edilmiş, 8 karakter).</param>
    /// <param name="machineName">Bilgisayar adı.</param>
    /// <param name="agentVersion">Ajan sürümü.</param>
    /// <param name="now">Şu an (UTC).</param>
    /// <param name="lifetime">Geçerlilik süresi.</param>
    /// <param name="intervalSeconds">Yoklama aralığı.</param>
    /// <returns>İstek.</returns>
    public static DevicePairing Start(
        string deviceCodeHash,
        string userCode,
        string machineName,
        string agentVersion,
        DateTimeOffset now,
        TimeSpan lifetime,
        int intervalSeconds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceCodeHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(userCode);
        return new DevicePairing
        {
            DeviceCodeHash = deviceCodeHash,
            UserCode = userCode,
            MachineName = machineName?.Trim() ?? string.Empty,
            AgentVersion = agentVersion?.Trim() ?? string.Empty,
            Status = DevicePairingStatus.Pending,
            ExpiresAtUtc = now + lifetime,
            IntervalSeconds = Math.Max(1, intervalSeconds),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }

    /// <summary>Süresi dolmuş mu.</summary>
    /// <param name="now">Şu an.</param>
    /// <returns>Dolmuşsa <c>true</c>.</returns>
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAtUtc;

    /// <summary>Kullanıcı onayı.</summary>
    /// <param name="userId">Onaylayan kullanıcı.</param>
    /// <param name="now">Şu an.</param>
    /// <returns>Sonuç.</returns>
    public Result Approve(Guid userId, DateTimeOffset now) => Decide(userId, now, DevicePairingStatus.Approved);

    /// <summary>Kullanıcı reddi.</summary>
    /// <param name="userId">Reddeden kullanıcı.</param>
    /// <param name="now">Şu an.</param>
    /// <returns>Sonuç.</returns>
    public Result Deny(Guid userId, DateTimeOffset now) => Decide(userId, now, DevicePairingStatus.Denied);

    /// <summary>Ajan yoklaması: süre, durum ve yoklama sıklığına göre sonuç döner; son yoklama anını günceller.</summary>
    /// <param name="now">Şu an.</param>
    /// <returns>Yoklama sonucu.</returns>
    public PairingPollOutcome Poll(DateTimeOffset now)
    {
        var previous = LastPolledAtUtc;
        LastPolledAtUtc = now;

        if (Status == DevicePairingStatus.Consumed)
        {
            return PairingPollOutcome.Expired;
        }

        if (Status == DevicePairingStatus.Denied)
        {
            return PairingPollOutcome.Denied;
        }

        if (IsExpired(now))
        {
            return PairingPollOutcome.Expired;
        }

        if (Status == DevicePairingStatus.Approved)
        {
            return PairingPollOutcome.Approved;
        }

        if (previous is not null && now - previous.Value < TimeSpan.FromSeconds(IntervalSeconds))
        {
            IntervalSeconds += SlowDownIncrementSeconds;
            return PairingPollOutcome.SlowDown;
        }

        return PairingPollOutcome.Pending;
    }

    /// <summary>Token verildi: isteği kapatır ve cihazı bağlar (tek seferlik).</summary>
    /// <param name="agentDeviceId">Cihaz kimliği.</param>
    /// <param name="now">Şu an.</param>
    /// <returns>Sonuç (onaylı değilse hata).</returns>
    public Result Consume(Guid agentDeviceId, DateTimeOffset now)
    {
        if (Status != DevicePairingStatus.Approved)
        {
            return AgentErrors.ExpiredToken;
        }

        Status = DevicePairingStatus.Consumed;
        AgentDeviceId = agentDeviceId;
        ConsumedAtUtc = now;
        return Result.Success();
    }

    private Result Decide(Guid userId, DateTimeOffset now, DevicePairingStatus target)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Kullanıcı kimliği boş olamaz.", nameof(userId));
        }

        if (Status != DevicePairingStatus.Pending)
        {
            return AgentErrors.PairingAlreadyCompleted;
        }

        if (IsExpired(now))
        {
            return AgentErrors.UserCodeExpired;
        }

        Status = target;
        UserId = userId;
        DecidedAtUtc = now;
        return Result.Success();
    }
}
