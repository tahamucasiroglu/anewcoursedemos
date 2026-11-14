using TahaKonsept.BuildingBlocks.Domain.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Base;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Domain.Entities;

/// <summary>
/// Kullanıcıya görünen işlem günlüğü kaydı (tablo <c>diagnostics.activity_logs</c>). Kalıcıdır (silinmez), güne göre sorgulanır.
/// Önemli alanlar: <see cref="Level"/>, <see cref="Source"/>, <see cref="Message"/>, <see cref="Details"/> (JSON), <see cref="DurationMs"/>.
/// Oluşturma: <see cref="Create"/> (alanları kırpar/normalleştirir; hiç hata vermez, çünkü günlük yazmak işi asla bozmamalı).
/// </summary>
public sealed class ActivityLog : AggregateRoot, IHasUser, IAuditable
{
    /// <summary>Kaynak adının en büyük uzunluğu.</summary>
    public const int MaxSourceLength = 64;

    /// <summary>Mesajın en büyük uzunluğu.</summary>
    public const int MaxMessageLength = 1000;

    /// <summary>Ayrıntı JSON metninin en büyük uzunluğu.</summary>
    public const int MaxDetailsLength = 32000;

    private ActivityLog()
    {
    }

    /// <summary>Kaydın sahibi (sistem kayıtlarında <see cref="Guid.Empty"/>).</summary>
    public Guid UserId { get; private set; }

    /// <summary>Seviye.</summary>
    public LogLevelKind Level { get; private set; }

    /// <summary>Kaynak (ör. <c>Production</c>).</summary>
    public string Source { get; private set; } = ActivitySources.System;

    /// <summary>Mesaj.</summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>Ek ayrıntılar (geçerli JSON metni) ya da <c>null</c>.</summary>
    public string? Details { get; private set; }

    /// <summary>İşlem süresi (ms) ya da <c>null</c>.</summary>
    public long? DurationMs { get; private set; }

    /// <summary>Oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Güncellenme anı (UTC).</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Yeni kayıt üretir; boş kaynak → <c>System</c>, uzun metinler kırpılır, negatif süre atılır.</summary>
    /// <param name="userId">Sahip (sistem için <see cref="Guid.Empty"/>).</param>
    /// <param name="level">Seviye.</param>
    /// <param name="source">Kaynak.</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="detailsJson">Ayrıntı (geçerli JSON metni) ya da <c>null</c>.</param>
    /// <param name="durationMs">Süre (ms) ya da <c>null</c>.</param>
    /// <returns>Kayıt.</returns>
    public static ActivityLog Create(Guid userId, LogLevelKind level, string? source, string? message, string? detailsJson = null, long? durationMs = null) => new()
    {
        UserId = userId,
        Level = level,
        Source = Clip(string.IsNullOrWhiteSpace(source) ? ActivitySources.System : source.Trim(), MaxSourceLength),
        Message = Clip(string.IsNullOrWhiteSpace(message) ? "(boş mesaj)" : message.Trim(), MaxMessageLength),
        Details = string.IsNullOrWhiteSpace(detailsJson) || detailsJson.Length > MaxDetailsLength ? null : detailsJson,
        DurationMs = durationMs is < 0 ? null : durationMs,
    };

    private static string Clip(string value, int max) => value.Length <= max ? value : value[..(max - 1)] + "…";
}
