using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Application.Concrete;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Modules.Production.Domain.Entities;
using TahaKonsept.Shared.Contracts.Concepts;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IPipelineContextFactory"/> ortak davranışı: işin ayar/konsept JSON'larını çözer, okunuş sözlüğünü kurar (önce kullanıcının
/// onaylı kayıtları, sonra konseptin önerileri; aynı yazılış tekrar edilmez) ve çıktı görünümünü işin çalıştırmalarından oluşturur.
/// </summary>
public abstract class PipelineContextFactoryBase : IPipelineContextFactory
{
    private readonly IPronunciationRepository _pronunciations;
    private readonly IScenePlanner _planner;

    /// <summary>Oluşturur.</summary>
    /// <param name="pronunciations">Okunuş sözlüğü deposu.</param>
    /// <param name="planner">Sahne planlayıcı (plan yedeği).</param>
    protected PipelineContextFactoryBase(IPronunciationRepository pronunciations, IScenePlanner planner)
    {
        _pronunciations = pronunciations ?? throw new ArgumentNullException(nameof(pronunciations));
        _planner = planner ?? throw new ArgumentNullException(nameof(planner));
    }

    /// <summary>Bağlamı kurar.</summary>
    /// <param name="job">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Bağlam.</returns>
    public async Task<PipelineContext> CreateAsync(Job job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        var settings = ProductionJson.Read<ProductionSettingsDto>(job.SettingsJson) ?? new ProductionSettingsDto();
        var document = ProductionJson.Read<ConceptDocument>(job.DocumentJson) ?? new ConceptDocument();
        var approved = await _pronunciations.ListApprovedAsync(job.UserId, cancellationToken);
        var lexicon = new List<PronunciationEntry>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in approved.Select(p => new PronunciationEntry { Written = p.Written, Spoken = p.Spoken }).Concat(document.Pronunciations))
        {
            if (!string.IsNullOrWhiteSpace(entry.Written) && !string.IsNullOrWhiteSpace(entry.Spoken) && seen.Add(Pronunciation.KeyOf(entry.Written)))
            {
                lexicon.Add(entry);
            }
        }

        return new PipelineContext
        {
            UserId = job.UserId,
            JobId = job.Id,
            VideoId = job.VideoId,
            WorkspaceId = job.WorkspaceId,
            Title = job.Title,
            Width = job.Width,
            Height = job.Height,
            Fps = job.Fps,
            Settings = settings,
            Document = document,
            Lexicon = lexicon,
            Outputs = new JobStageOutputView(job.Runs, () => _planner.Plan(document, settings.ImageMode != MediaMode.Off)),
        };
    }
}
