using Konsept.Modules.Production.Domain.Enums;

namespace Konsept.Modules.Production.Domain.Entities;

/// <summary>
/// Üretim sırasındaki iş (spec 0006 Req 10–15): sahibi ve profili (BR-1, BR-2), konsept, başlık ve konseptin <b>kaydındaki</b> dil/biçim/süre
/// (sıraya alınırken kopyalanır; Req 20), profil içindeki sıra konumu, durum ve <i>Üretiliyor</i>'a geçişte sabitlenen ayarlar.
/// </summary>
public sealed record ProductionJob
{
    /// <summary>Gets kimlik (UUIDv7).</summary>
    public Guid Id { get; init; }

    /// <summary>Gets sahibi.</summary>
    public Guid UserId { get; init; }

    /// <summary>Gets profil.</summary>
    public Guid ProfileId { get; init; }

    /// <summary>Gets konsept.</summary>
    public Guid ConceptId { get; init; }

    /// <summary>Gets konsept başlığı (sıradaki konsept düzenlenemez; 0015 başlığı değiştirmez).</summary>
    public required string Title { get; init; }

    /// <summary>Gets konseptin kaydındaki video dili.</summary>
    public required string Language { get; init; }

    /// <summary>Gets konseptin kaydındaki biçim.</summary>
    public required string Format { get; init; }

    /// <summary>Gets konseptin kaydındaki hedef süre (saniye).</summary>
    public int TargetDurationSeconds { get; init; }

    /// <summary>Gets profil sırasındaki konum (küçük üstte; aralıklı olabilir).</summary>
    public long Position { get; init; }

    /// <summary>Gets durum.</summary>
    public ProductionJobStatus Status { get; init; }

    /// <summary>Gets sabitlenen ayarlar; iş hiç başlamadıysa <c>null</c>.</summary>
    public FixedJobSettings? FixedSettings { get; init; }

    /// <summary>Gets hat ilerlemesi (spec 0017); iş hiç başlamadıysa <c>null</c>.</summary>
    public JobProgress? Progress { get; init; }

    /// <summary>Gets oluşturulma (UTC).</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets son güncelleme (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; init; }
}
