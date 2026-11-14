using TahaKonsept.BuildingBlocks.Domain.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Base;
using TahaKonsept.Modules.Production.Domain.Constants;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Domain.Entities;

/// <summary>
/// Üretim sırasındaki bir öğe: konsept, sıra numarası, durum ve (varsa) etkin iş. Yeni sürüm / yeniden üret için
/// <see cref="BaseJobId"/> (çıktıları yeniden kullanılacak iş), <see cref="ParentVideoId"/>, <see cref="Version"/>, ayar ve konsept
/// geçersiz kılmaları (<see cref="SettingsJson"/>, <see cref="DocumentJson"/>) taşır. Durum geçişleri: <see cref="MarkRunning"/>,
/// <see cref="MarkPaused"/>, <see cref="MarkDone"/>, <see cref="MarkFailed"/>, <see cref="Cancel"/>.
/// </summary>
public sealed class QueueItem : AggregateRoot, IHasUser, IAuditable
{
    /// <summary>EF Core için boş kurucu.</summary>
    private QueueItem()
    {
    }

    /// <summary>Sahip kullanıcı.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Konsept.</summary>
    public Guid ConceptId { get; private set; }

    /// <summary>Başlık.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Sıra (küçük önce).</summary>
    public int Order { get; private set; }

    /// <summary>Durum.</summary>
    public QueueItemStatus Status { get; private set; } = QueueItemStatus.Waiting;

    /// <summary>Etkin/son iş.</summary>
    public Guid? JobId { get; private set; }

    /// <summary>Oluşan video.</summary>
    public Guid? VideoId { get; private set; }

    /// <summary>Ayar geçersiz kılma (JSON; <c>null</c> = iş başlarken kullanıcının güncel ayarı).</summary>
    public string? SettingsJson { get; private set; }

    /// <summary>Konsept geçersiz kılma (JSON; <c>null</c> = konseptin güncel hali).</summary>
    public string? DocumentJson { get; private set; }

    /// <summary>Çıktıları yeniden kullanılacak iş (yeni sürüm).</summary>
    public Guid? BaseJobId { get; private set; }

    /// <summary>Kaynak video.</summary>
    public Guid? ParentVideoId { get; private set; }

    /// <summary>Sürüm numarası.</summary>
    public int Version { get; private set; } = 1;

    /// <summary>Durum mesajı.</summary>
    public string? Message { get; private set; }

    /// <summary>Başlama anı (UTC).</summary>
    public DateTimeOffset? StartedAtUtc { get; private set; }

    /// <summary>Bitiş anı (UTC).</summary>
    public DateTimeOffset? FinishedAtUtc { get; private set; }

    /// <summary>Oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Güncellenme anı (UTC).</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Etkin (çalışan ya da duraklatılmış) mı.</summary>
    public bool IsActive => Status is QueueItemStatus.Running or QueueItemStatus.Paused;

    /// <summary>Yeni öğe oluşturur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="title">Başlık.</param>
    /// <param name="order">Sıra.</param>
    /// <param name="settingsJson">Ayar geçersiz kılma.</param>
    /// <param name="documentJson">Konsept geçersiz kılma.</param>
    /// <param name="baseJobId">Yeniden kullanılacak iş.</param>
    /// <param name="parentVideoId">Kaynak video.</param>
    /// <param name="version">Sürüm.</param>
    /// <returns>Öğe.</returns>
    public static QueueItem Create(
        Guid userId,
        Guid conceptId,
        string title,
        int order,
        string? settingsJson = null,
        string? documentJson = null,
        Guid? baseJobId = null,
        Guid? parentVideoId = null,
        int version = 1) =>
        new()
        {
            UserId = userId,
            ConceptId = conceptId,
            Title = ProductionLimits.Clip(title, ProductionLimits.TitleMaxLength),
            Order = order,
            SettingsJson = settingsJson,
            DocumentJson = documentJson,
            BaseJobId = baseJobId,
            ParentVideoId = parentVideoId,
            Version = Math.Max(1, version),
        };

    /// <summary>Sıra numarasını değiştirir.</summary>
    /// <param name="order">Yeni sıra.</param>
    public void SetOrder(int order) => Order = order;

    /// <summary>Başlığı günceller (boşsa değişmez).</summary>
    /// <param name="title">Başlık.</param>
    public void Rename(string? title)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            Title = ProductionLimits.Clip(title, ProductionLimits.TitleMaxLength);
        }
    }

    /// <summary>Çalışıyor olarak işaretler.</summary>
    /// <param name="jobId">İş.</param>
    /// <param name="nowUtc">Şimdi.</param>
    public void MarkRunning(Guid jobId, DateTimeOffset nowUtc)
    {
        JobId = jobId;
        Status = QueueItemStatus.Running;
        StartedAtUtc ??= nowUtc;
        Message = null;
    }

    /// <summary>Duraklatıldı olarak işaretler (yalnızca çalışıyorsa).</summary>
    public void MarkPaused()
    {
        if (Status == QueueItemStatus.Running)
        {
            Status = QueueItemStatus.Paused;
            Message = "Durduruldu";
        }
    }

    /// <summary>Tamamlandı olarak işaretler.</summary>
    /// <param name="videoId">Oluşan video.</param>
    /// <param name="nowUtc">Şimdi.</param>
    public void MarkDone(Guid videoId, DateTimeOffset nowUtc)
    {
        VideoId = videoId;
        Status = QueueItemStatus.Done;
        FinishedAtUtc = nowUtc;
        Message = "Video hazır";
    }

    /// <summary>Hata ile bitti olarak işaretler.</summary>
    /// <param name="message">Hata mesajı.</param>
    /// <param name="nowUtc">Şimdi.</param>
    public void MarkFailed(string message, DateTimeOffset nowUtc)
    {
        Status = QueueItemStatus.Failed;
        FinishedAtUtc = nowUtc;
        Message = ProductionLimits.Clip(message, ProductionLimits.MessageMaxLength);
    }

    /// <summary>İptal eder.</summary>
    /// <param name="nowUtc">Şimdi.</param>
    public void Cancel(DateTimeOffset nowUtc)
    {
        Status = QueueItemStatus.Cancelled;
        FinishedAtUtc = nowUtc;
        Message = "İptal edildi";
    }

    /// <summary>Durum mesajını günceller.</summary>
    /// <param name="message">Mesaj.</param>
    public void SetMessage(string? message) => Message = ProductionLimits.ClipOrNull(message, ProductionLimits.MessageMaxLength);
}
