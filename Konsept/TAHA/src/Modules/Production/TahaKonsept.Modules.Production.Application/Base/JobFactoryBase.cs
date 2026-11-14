using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Domain.Entities;
using TahaKonsept.Modules.Production.Domain.Models;
using TahaKonsept.Modules.Profiles.Application.Abstract;
using TahaKonsept.Shared.Contracts.Concepts;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IJobFactory"/> ortak davranışı:
/// <list type="number">
/// <item>Ayar: öğenin geçersiz kılması ya da kullanıcının güncel ayarı (düzeltilmiş).</item>
/// <item>Konsept: öğenin konsept JSON'u ya da <see cref="IConceptSource"/>; profil biliniyorsa biçim profilden (yoksa konseptten) → çözünürlük.</item>
/// <item>Aşama çalıştırmaları hat sırasıyla eklenir (sahne bazlılar sahne başına).</item>
/// <item>Yeni sürümde <see cref="IReusePlanner"/> ile plan çıkarılır; online ses/görsel yeniden üretilecekse ajan çalışma alanı paylaşılmaz
/// (eski aynı adlı dosyalar karışmasın), değilse kaynak işin alanı paylaşılır (ajan değişmeyen sahne/ses dosyalarını atlar).</item>
/// <item>Konseptin okunuş önerileri sözlüğe öneri olarak düşer.</item>
/// </list>
/// </summary>
public abstract class JobFactoryBase : IJobFactory
{
    private readonly IProductionSettingsService _settings;
    private readonly IConceptSource _concepts;
    private readonly IProfileReader _profiles;
    private readonly IJobRepository _jobs;
    private readonly IPipelineContextFactory _contexts;
    private readonly IStageHandlerRegistry _handlers;
    private readonly IReusePlanner _reuse;
    private readonly IPronunciationSuggestions _suggestions;

    /// <summary>Oluşturur.</summary>
    /// <param name="settings">Ayar servisi.</param>
    /// <param name="concepts">Konsept portu.</param>
    /// <param name="profiles">Profil okuyucu.</param>
    /// <param name="jobs">İş deposu.</param>
    /// <param name="contexts">Bağlam fabrikası.</param>
    /// <param name="handlers">Aşama işleyicileri.</param>
    /// <param name="reuse">Yeniden kullanım planlayıcı.</param>
    /// <param name="suggestions">Okunuş önerileri.</param>
    protected JobFactoryBase(
        IProductionSettingsService settings,
        IConceptSource concepts,
        IProfileReader profiles,
        IJobRepository jobs,
        IPipelineContextFactory contexts,
        IStageHandlerRegistry handlers,
        IReusePlanner reuse,
        IPronunciationSuggestions suggestions)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _concepts = concepts ?? throw new ArgumentNullException(nameof(concepts));
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));
        _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        _reuse = reuse ?? throw new ArgumentNullException(nameof(reuse));
        _suggestions = suggestions ?? throw new ArgumentNullException(nameof(suggestions));
    }

    /// <summary>İşi oluşturur.</summary>
    /// <param name="item">Sıra öğesi.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İş ya da hata.</returns>
    public async Task<Result<Job>> CreateAsync(QueueItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        var userId = item.UserId;
        var settings = item.SettingsJson is null
            ? await _settings.GetForUserAsync(userId, cancellationToken)
            : ProductionSettingsDefaults.Normalize(ProductionJson.Read<ProductionSettingsDto>(item.SettingsJson));

        ConceptDocument? document = ProductionJson.Read<ConceptDocument>(item.DocumentJson);
        Guid? profileId = null;
        var title = item.Title;
        if (document is null)
        {
            var snapshot = await _concepts.GetAsync(userId, item.ConceptId, cancellationToken);
            if (snapshot is null)
            {
                return ProductionErrors.ConceptNotFound;
            }

            document = snapshot.Document;
            profileId = snapshot.ProfileId;
            title = string.IsNullOrWhiteSpace(snapshot.Title) ? title : snapshot.Title;
        }

        if (document.Scenes.Count == 0)
        {
            return ProductionErrors.ConceptHasNoScenes;
        }

        title = string.IsNullOrWhiteSpace(document.Title) ? title : document.Title;
        var format = document.Format;
        if (profileId is Guid pid)
        {
            var profile = await _profiles.GetAsync(pid, cancellationToken);
            format = profile?.Format ?? format;
        }

        var (width, height) = ProductionSettingsDefaults.Resolution(format);
        var job = Job.Create(new JobSpec
        {
            UserId = userId,
            QueueItemId = item.Id,
            ConceptId = item.ConceptId,
            ProfileId = profileId,
            Title = string.IsNullOrWhiteSpace(title) ? "Adsız video" : title,
            BaseJobId = item.BaseJobId,
            ParentVideoId = item.ParentVideoId,
            Version = item.Version,
            Format = format,
            Width = width,
            Height = height,
            Fps = settings.Fps,
            SettingsJson = ProductionJson.Write(settings),
            DocumentJson = ProductionJson.Write(document),
        });

        var context = await _contexts.CreateAsync(job, cancellationToken);
        foreach (var handler in _handlers.All)
        {
            if (handler.PerScene)
            {
                for (var i = 0; i < document.Scenes.Count; i++)
                {
                    job.AddRun(handler.Stage, i, handler.ResolveLocation(context));
                }
            }
            else
            {
                job.AddRun(handler.Stage, null, handler.ResolveLocation(context));
            }
        }

        if (item.BaseJobId is Guid baseId)
        {
            var baseJob = await _jobs.GetForUserNoTrackingAsync(baseId, userId, cancellationToken);
            if (baseJob is not null)
            {
                var plan = await _reuse.PlanAsync(context, baseJob.Runs, cancellationToken);
                if (!NeedsFreshWorkspace(settings, plan.RerunStages))
                {
                    job.ShareWorkspace(baseJob.WorkspaceId);
                }
            }
        }

        await _suggestions.AddSuggestionsAsync(userId, item.ConceptId, title, document.Pronunciations, cancellationToken);
        await _jobs.AddAsync(job, cancellationToken);
        return job;
    }

    /// <summary>
    /// Ajan çalışma alanı paylaşılamaz mı: online ses ya da online görsel yeniden üretilecekse (aynı adlı eski indirmeler karışır)
    /// veya ses kapatıldıysa (eski altyazı/ses dosyaları kalmasın).
    /// </summary>
    /// <param name="settings">Yeni ayarlar.</param>
    /// <param name="rerun">Yeniden çalışacak aşamalar.</param>
    /// <returns>Yeni alan gerekiyorsa <c>true</c>.</returns>
    protected virtual bool NeedsFreshWorkspace(ProductionSettingsDto settings, IReadOnlyList<PipelineStage> rerun)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(rerun);
        return (rerun.Contains(PipelineStage.Speech) && settings.SpeechMode != MediaMode.Local)
            || (rerun.Contains(PipelineStage.Images) && settings.ImageMode == MediaMode.Online);
    }
}
