using TahaKonsept.BuildingBlocks.Domain.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Base;
using TahaKonsept.Modules.Production.Domain.Constants;
using TahaKonsept.Modules.Production.Domain.Models;
using TahaKonsept.Shared.Contracts.Concepts;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Domain.Entities;

/// <summary>
/// Üretim işi aggregate'i (bir video sürümü): konsept ve ayar anlık görüntüleri (<see cref="DocumentJson"/>, <see cref="SettingsJson"/>),
/// çözünürlük, önceden atanmış <see cref="VideoId"/>, ajan çalışma alanı kimliği (<see cref="WorkspaceId"/>; yeni sürümde kaynak işin alanı
/// paylaşılır ki ajan değişmeyen dosyaları atlasın), <see cref="BaseJobId"/> ve aşama çalıştırmaları (<see cref="Runs"/>).
/// Geçişler: <see cref="Start"/>, <see cref="Pause"/>, <see cref="Resume"/>, <see cref="Complete"/>, <see cref="Fail"/>, <see cref="Cancel"/>,
/// <see cref="SetWaitingForAgent"/>.
/// </summary>
public sealed class Job : AggregateRoot, IHasUser, IAuditable
{
    private readonly List<StageRun> _runs = [];

    /// <summary>EF Core için boş kurucu.</summary>
    private Job()
    {
    }

    /// <summary>Sahip kullanıcı.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Sıra öğesi.</summary>
    public Guid QueueItemId { get; private set; }

    /// <summary>Konsept.</summary>
    public Guid ConceptId { get; private set; }

    /// <summary>Profil (biliniyorsa).</summary>
    public Guid? ProfileId { get; private set; }

    /// <summary>Başlık.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Durum.</summary>
    public JobStatus Status { get; private set; } = JobStatus.Queued;

    /// <summary>Durum mesajı.</summary>
    public string? StatusMessage { get; private set; }

    /// <summary>Masaüstü programı bekleniyor mu.</summary>
    public bool WaitingForAgent { get; private set; }

    /// <summary>Oluşacak video kimliği.</summary>
    public Guid VideoId { get; private set; }

    /// <summary>Ajan çalışma alanı kimliği (<c>StageCommand.JobId</c> olarak gönderilir).</summary>
    public Guid WorkspaceId { get; private set; }

    /// <summary>Çıktıları yeniden kullanılan iş.</summary>
    public Guid? BaseJobId { get; private set; }

    /// <summary>Kaynak video.</summary>
    public Guid? ParentVideoId { get; private set; }

    /// <summary>Sürüm.</summary>
    public int Version { get; private set; } = 1;

    /// <summary>Biçim.</summary>
    public VideoFormat Format { get; private set; }

    /// <summary>Genişlik (px).</summary>
    public int Width { get; private set; }

    /// <summary>Yükseklik (px).</summary>
    public int Height { get; private set; }

    /// <summary>Kare hızı.</summary>
    public int Fps { get; private set; }

    /// <summary>Ayar anlık görüntüsü (JSON).</summary>
    public string SettingsJson { get; private set; } = "{}";

    /// <summary>Konsept anlık görüntüsü (JSON).</summary>
    public string DocumentJson { get; private set; } = "{}";

    /// <summary>Başlama (UTC).</summary>
    public DateTimeOffset? StartedAtUtc { get; private set; }

    /// <summary>Bitiş (UTC).</summary>
    public DateTimeOffset? FinishedAtUtc { get; private set; }

    /// <summary>Oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Güncellenme anı (UTC).</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Aşama çalıştırmaları.</summary>
    public IReadOnlyList<StageRun> Runs => _runs;

    /// <summary>Toplam maliyet ($).</summary>
    public decimal TotalCostUsd => _runs.Sum(r => r.CostUsd);

    /// <summary>Hat sırasına göre çalıştırmalar.</summary>
    public IEnumerable<StageRun> OrderedRuns => _runs.OrderBy(r => r.Sequence);

    /// <summary>Yeni iş oluşturur.</summary>
    /// <param name="spec">İş tanımı.</param>
    /// <returns>İş.</returns>
    public static Job Create(JobSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var job = new Job
        {
            UserId = spec.UserId,
            QueueItemId = spec.QueueItemId,
            ConceptId = spec.ConceptId,
            ProfileId = spec.ProfileId,
            Title = ProductionLimits.Clip(spec.Title, ProductionLimits.TitleMaxLength),
            VideoId = Guid.CreateVersion7(),
            BaseJobId = spec.BaseJobId,
            ParentVideoId = spec.ParentVideoId,
            Version = Math.Max(1, spec.Version),
            Format = spec.Format,
            Width = spec.Width,
            Height = spec.Height,
            Fps = spec.Fps,
            SettingsJson = spec.SettingsJson,
            DocumentJson = spec.DocumentJson,
        };
        job.WorkspaceId = spec.WorkspaceId ?? job.Id;
        return job;
    }

    /// <summary>Çalıştırma ekler.</summary>
    /// <param name="stage">Aşama.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <param name="location">Varsayılan yer.</param>
    /// <returns>Çalıştırma.</returns>
    public StageRun AddRun(PipelineStage stage, int? sceneIndex, StageLocation location)
    {
        var run = StageRun.Create(Id, _runs.Count, stage, sceneIndex, location);
        _runs.Add(run);
        return run;
    }

    /// <summary>Çalıştırmayı bulur.</summary>
    /// <param name="stage">Aşama.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <returns>Çalıştırma ya da <c>null</c>.</returns>
    public StageRun? FindRun(PipelineStage stage, int? sceneIndex = null) =>
        _runs.FirstOrDefault(r => r.Stage == stage && r.SceneIndex == sceneIndex);

    /// <summary>Başlatır.</summary>
    /// <param name="nowUtc">Şimdi.</param>
    public void Start(DateTimeOffset nowUtc)
    {
        Status = JobStatus.Running;
        StartedAtUtc ??= nowUtc;
        StatusMessage = "Başladı";
    }

    /// <summary>Duraklatır.</summary>
    public void Pause()
    {
        if (Status == JobStatus.Running)
        {
            Status = JobStatus.Paused;
            StatusMessage = "Durduruldu";
            WaitingForAgent = false;
        }
    }

    /// <summary>Sürdürür.</summary>
    public void Resume()
    {
        if (Status == JobStatus.Paused)
        {
            Status = JobStatus.Running;
            StatusMessage = "Devam ediyor";
        }
    }

    /// <summary>Durum mesajını günceller.</summary>
    /// <param name="message">Mesaj.</param>
    public void SetMessage(string? message) => StatusMessage = ProductionLimits.ClipOrNull(message, ProductionLimits.MessageMaxLength);

    /// <summary>Ajan bekleme durumunu ayarlar.</summary>
    /// <param name="waiting">Bekleniyor mu.</param>
    public void SetWaitingForAgent(bool waiting)
    {
        WaitingForAgent = waiting;
        if (waiting)
        {
            StatusMessage = "Ajan bekleniyor";
        }
    }

    /// <summary>Tamamlar.</summary>
    /// <param name="nowUtc">Şimdi.</param>
    public void Complete(DateTimeOffset nowUtc)
    {
        Status = JobStatus.Completed;
        FinishedAtUtc = nowUtc;
        WaitingForAgent = false;
        StatusMessage = "Video hazır";
    }

    /// <summary>Hata ile bitirir.</summary>
    /// <param name="message">Mesaj.</param>
    /// <param name="nowUtc">Şimdi.</param>
    public void Fail(string message, DateTimeOffset nowUtc)
    {
        Status = JobStatus.Failed;
        FinishedAtUtc = nowUtc;
        WaitingForAgent = false;
        StatusMessage = ProductionLimits.Clip(message, ProductionLimits.MessageMaxLength);
    }

    /// <summary>İptal eder (bitmemiş çalıştırmalar da iptal olur).</summary>
    /// <param name="nowUtc">Şimdi.</param>
    public void Cancel(DateTimeOffset nowUtc)
    {
        Status = JobStatus.Cancelled;
        FinishedAtUtc = nowUtc;
        WaitingForAgent = false;
        StatusMessage = "İptal edildi";
        foreach (var run in _runs)
        {
            run.Cancel(nowUtc);
        }
    }
}
