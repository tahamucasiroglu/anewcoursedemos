using TahaKonsept.BuildingBlocks.Domain.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Base;
using TahaKonsept.Modules.Production.Domain.Constants;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Production.Domain.Entities;

/// <summary>
/// Üretilen video kaydı (dosyanın kendisi kullanıcının bilgisayarında, ajanda; kimlik = ajandaki video kimliği <see cref="AgentVideoId"/>).
/// Önemli alanlar: süre, toplam maliyet + aşama dökümü (<see cref="CostBreakdownJson"/>), ayar ve konsept anlık görüntüleri,
/// sahneler (yazılış + okunuş, <see cref="ScenesJson"/>), <see cref="Version"/> / <see cref="ParentVideoId"/>.
/// </summary>
public sealed class Video : AggregateRoot, IHasUser, IAuditable
{
    /// <summary>EF Core için boş kurucu.</summary>
    private Video()
    {
    }

    private Video(Guid id)
        : base(id)
    {
    }

    /// <summary>Sahip kullanıcı.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Konsept.</summary>
    public Guid ConceptId { get; private set; }

    /// <summary>Üreten iş.</summary>
    public Guid JobId { get; private set; }

    /// <summary>Başlık.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Biçim.</summary>
    public VideoFormat Format { get; private set; }

    /// <summary>Genişlik.</summary>
    public int Width { get; private set; }

    /// <summary>Yükseklik.</summary>
    public int Height { get; private set; }

    /// <summary>Kare hızı.</summary>
    public int Fps { get; private set; }

    /// <summary>Süre (sn).</summary>
    public double DurationSec { get; private set; }

    /// <summary>Toplam maliyet ($).</summary>
    public decimal TotalCostUsd { get; private set; }

    /// <summary>Aşama maliyet/zaman dökümü (JSON).</summary>
    public string CostBreakdownJson { get; private set; } = "[]";

    /// <summary>Ayar anlık görüntüsü (JSON).</summary>
    public string SettingsJson { get; private set; } = "{}";

    /// <summary>Konsept anlık görüntüsü (JSON).</summary>
    public string DocumentJson { get; private set; } = "{}";

    /// <summary>Sahneler (JSON).</summary>
    public string ScenesJson { get; private set; } = "[]";

    /// <summary>Ajandaki video kimliği.</summary>
    public Guid AgentVideoId { get; private set; }

    /// <summary>Sürüm.</summary>
    public int Version { get; private set; } = 1;

    /// <summary>Kaynak video.</summary>
    public Guid? ParentVideoId { get; private set; }

    /// <summary>Üretim başlangıcı (UTC).</summary>
    public DateTimeOffset? StartedAtUtc { get; private set; }

    /// <summary>Üretim bitişi (UTC).</summary>
    public DateTimeOffset? FinishedAtUtc { get; private set; }

    /// <summary>Oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Güncellenme anı (UTC).</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Bitmiş işten video kaydı oluşturur (kimlik = iş için önceden atanan video kimliği).</summary>
    /// <param name="job">İş.</param>
    /// <param name="durationSec">Süre.</param>
    /// <param name="costBreakdownJson">Maliyet dökümü.</param>
    /// <param name="scenesJson">Sahneler.</param>
    /// <param name="finishedAtUtc">Bitiş.</param>
    /// <returns>Video.</returns>
    public static Video FromJob(Job job, double durationSec, string costBreakdownJson, string scenesJson, DateTimeOffset finishedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(job);
        return new Video(job.VideoId)
        {
            UserId = job.UserId,
            ConceptId = job.ConceptId,
            JobId = job.Id,
            Title = ProductionLimits.Clip(job.Title, ProductionLimits.TitleMaxLength),
            Format = job.Format,
            Width = job.Width,
            Height = job.Height,
            Fps = job.Fps,
            DurationSec = Math.Round(durationSec, 2),
            TotalCostUsd = job.TotalCostUsd,
            CostBreakdownJson = costBreakdownJson,
            SettingsJson = job.SettingsJson,
            DocumentJson = job.DocumentJson,
            ScenesJson = scenesJson,
            AgentVideoId = job.VideoId,
            Version = job.Version,
            ParentVideoId = job.ParentVideoId,
            StartedAtUtc = job.StartedAtUtc,
            FinishedAtUtc = finishedAtUtc,
        };
    }
}
