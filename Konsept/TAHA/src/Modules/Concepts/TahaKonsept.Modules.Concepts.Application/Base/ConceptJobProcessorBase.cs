using System.Globalization;
using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Models;
using TahaKonsept.Modules.Concepts.Domain.Entities;
using TahaKonsept.Modules.Concepts.Domain.Enums;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Shared.Contracts.Concepts;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Concepts.Application.Base;

/// <summary>
/// <see cref="IConceptJobProcessor"/> ortak davranışı. Akış: bağlam → plan (<see cref="PlanAsync"/>: başlıklar ya da fikirden N açı) →
/// her öğe: bütçe denetimi (<c>IBudgetGuard</c>; iş kanalda beklemiş olabilir, her konsept ayrı ücretli çağrı) → üret → (açıksa) kelime denetimi →
/// kaydet → hafıza → ilerleme. Bütçe aşılınca iş durur, kalanlar üretilemedi sayılır. Anahtar eksik / model yok gibi
/// <see cref="IsFatal"/> hatalarda iş durur; diğer hatalarda öğe atlanır. Sonunda "N konsept üretildi ($x) — süre …" günlüğe yazılır.
/// </summary>
public abstract class ConceptJobProcessorBase : IConceptJobProcessor
{
    private static readonly HashSet<string> FatalCodes = new(StringComparer.Ordinal)
    {
        ProviderErrorCodes.KeyMissing,
        ProviderErrorCodes.KeyCorrupt,
        ProviderErrorCodes.Unauthorized,
        ProviderErrorCodes.ModelNotFound,
        ProviderErrorCodes.ModelNotSupported,
        ProviderErrorCodes.UnknownPlatform,
        ProviderErrorCodes.BudgetExceeded,
        "concepts.no_profile",
    };

    /// <summary>Oluşturur.</summary>
    /// <param name="services">Bağımlılıklar.</param>
    protected ConceptJobProcessorBase(ConceptJobProcessorDependencies services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>Bağımlılıklar.</summary>
    protected ConceptJobProcessorDependencies Services { get; }

    /// <summary>İşi çalıştırır.</summary>
    /// <param name="job">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public virtual async Task ProcessAsync(ConceptJob job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        var started = Services.Clock.UtcNow;
        Services.Tracker.Update(job.Id, d => d with { Status = ConceptJobStatus.Running, StartedAt = started, Message = null });
        Services.Logger.LogInformation("Konsept işi başladı {JobId} ({Kind}, {Count} adet)", job.Id, job.Kind, job.Count);

        var produced = 0;
        var failed = 0;
        decimal cost = 0;
        string? lastError = null;
        var cancelled = false;
        try
        {
            var contextResult = await Services.Contexts.ResolveAsync(job.UserId, job.ProfileId, cancellationToken);
            if (contextResult.IsFailure)
            {
                await FinishAsync(job, started, 0, 0, 0, contextResult.Error.Message, false);
                return;
            }

            var allowed = await Services.Budget.EnsureCanStartProductionAsync(job.UserId, cancellationToken);
            if (allowed.IsFailure)
            {
                await FinishAsync(job, started, 0, 0, 0, allowed.Error.Message, false);
                return;
            }

            var context = contextResult.Value;
            var plan = await PlanAsync(job, context, cancellationToken);
            cost += plan.CostUsd;
            Services.Tracker.Update(job.Id, d => d with { Total = plan.Briefs.Count, CostUsd = cost });

            for (var index = 0; index < plan.Briefs.Count; index++)
            {
                var brief = plan.Briefs[index];
                cancellationToken.ThrowIfCancellationRequested();
                var budget = await Services.Budget.EnsureCanStartProductionAsync(job.UserId, cancellationToken);
                if (budget.IsFailure)
                {
                    failed += plan.Briefs.Count - index;
                    lastError = budget.Error.Message;
                    var stoppedFailures = failed;
                    var stoppedMessage = lastError;
                    Services.Tracker.Update(job.Id, d => d with { Failed = stoppedFailures, Message = stoppedMessage });
                    Services.Logger.LogWarning("Bütçe aşıldı; konsept işi durdu {JobId}: {Message}", job.Id, lastError);
                    break;
                }

                Services.Tracker.Update(job.Id, d => d with { CurrentTitle = brief.DisplayName });
                var item = await ProduceAsync(job, context, brief, cancellationToken);
                if (item.IsSuccess)
                {
                    produced++;
                    cost += item.Value.CostUsd;
                    var conceptId = item.Value.ConceptId;
                    var total = cost;
                    var done = produced;
                    Services.Tracker.Update(job.Id, d => d with { Completed = done, CostUsd = total, ProducedIds = [.. d.ProducedIds, conceptId] });
                    continue;
                }

                failed++;
                lastError = item.Error.Message;
                var failures = failed;
                var message = lastError;
                Services.Tracker.Update(job.Id, d => d with { Failed = failures, Message = message });
                Services.Logger.LogWarning("Konsept üretilemedi {JobId} '{Title}': {Code} {Message}", job.Id, brief.DisplayName, item.Error.Code, item.Error.Message);
                if (IsFatal(item.Error))
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            cancelled = true;
        }

        await FinishAsync(job, started, produced, failed, cost, lastError, cancelled);
    }

    /// <summary>Üretim planı: başlık işinde başlıklar; fikir işinde 1 adetse fikrin kendisi, fazlaysa konu üreticisiyle N farklı açı
    /// (başarısızsa yalnız açı numarasıyla).</summary>
    /// <param name="job">İş.</param>
    /// <param name="context">Bağlam.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Plan.</returns>
    protected virtual async Task<BriefPlan> PlanAsync(ConceptJob job, ConceptGenerationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        if (job.Kind == ConceptJobKind.Topics)
        {
            return new BriefPlan(job.Titles.Select(t => new ConceptBrief { Title = t, Origin = ConceptOrigin.Topic }).ToList(), 0);
        }

        if (job.Count <= 1)
        {
            return new BriefPlan([new ConceptBrief { Idea = job.Idea, Origin = ConceptOrigin.Idea }], 0);
        }

        var memory = await Services.Memory.LoadAsync(job.UserId, context.ProfileId, cancellationToken);
        var angles = await Services.Topics.GenerateAsync(context, job.Count, null, job.Idea, memory, cancellationToken);
        var briefs = new List<ConceptBrief>(job.Count);
        decimal cost = 0;
        if (angles.IsSuccess)
        {
            cost = angles.Value.CostUsd;
            briefs.AddRange(angles.Value.Items.Select((t, i) => new ConceptBrief
            {
                Title = t.Title,
                Hook = t.Hook,
                Idea = job.Idea,
                AngleNumber = i + 1,
                Origin = ConceptOrigin.Idea,
            }));
        }
        else
        {
            Services.Logger.LogWarning("Fikir açıları üretilemedi {JobId}: {Message}", job.Id, angles.Error.Message);
        }

        for (var n = briefs.Count + 1; n <= job.Count; n++)
        {
            briefs.Add(new ConceptBrief { Idea = job.Idea, AngleNumber = n, Origin = ConceptOrigin.Idea });
        }

        return new BriefPlan(briefs, cost);
    }

    /// <summary>Tek konsepti üretir ve kaydeder.</summary>
    /// <param name="job">İş.</param>
    /// <param name="context">Bağlam.</param>
    /// <param name="brief">Girdi.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kaydedilen konsept ya da hata.</returns>
    protected virtual async Task<Result<ProducedConcept>> ProduceAsync(ConceptJob job, ConceptGenerationContext context, ConceptBrief brief, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(brief);
        var generated = await Services.Generator.GenerateAsync(context, brief, cancellationToken);
        if (generated.IsFailure)
        {
            return generated.Error;
        }

        var g = generated.Value;
        var document = g.Document;
        long input = g.InputTokens, output = g.OutputTokens;
        var cost = g.CostUsd;
        if (context.Preferences.WordRepetitionCheck)
        {
            var check = await Services.WordCheck.CheckAsync(context, document, cancellationToken);
            if (check.IsSuccess)
            {
                document = check.Value.Document;
                input += check.Value.InputTokens;
                output += check.Value.OutputTokens;
                cost += check.Value.CostUsd;
            }
            else
            {
                Services.Logger.LogWarning("Kelime denetimi yapılamadı: {Message}", check.Error.Message);
            }
        }

        if (context.FactGrounding && document.Sources.Count == 0)
        {
            await Services.Activity.WarningAsync(ActivitySources.Concepts, $"Kaynaksız konsept (gerçek bilgi açık): {document.Title}", userId: job.UserId, cancellationToken: CancellationToken.None);
        }

        var concept = Concept.Create(job.UserId, context.ProfileId, brief.Origin, document, g.ModelKey, input, output, cost);
        await Services.Concepts.AddAsync(concept, cancellationToken);
        await Services.Concepts.UnitOfWork.SaveChangesAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(brief.Title))
        {
            await Services.Memory.RememberTitlesAsync(job.UserId, context.ProfileId, [brief.Title], TopicMemoryKind.Generated, CancellationToken.None);
        }

        await Services.Memory.MarkConceptAsync(job.UserId, context.ProfileId, concept.Id, concept.Title, TopicMemoryKind.Generated, false, CancellationToken.None);
        return new ProducedConcept(concept.Id, concept.Title, cost);
    }

    /// <summary>İşi durduran hata mı (anahtar, yetki, model, bütçe, profil).</summary>
    /// <param name="error">Hata.</param>
    /// <returns>Durdurur mu.</returns>
    protected virtual bool IsFatal(Error error) => error is not null && FatalCodes.Contains(error.Code);

    private async Task FinishAsync(ConceptJob job, DateTimeOffset started, int produced, int failed, decimal cost, string? lastError, bool cancelled)
    {
        var finished = Services.Clock.UtcNow;
        var status = cancelled ? ConceptJobStatus.Cancelled : produced > 0 ? ConceptJobStatus.Completed : ConceptJobStatus.Failed;
        var message = status switch
        {
            ConceptJobStatus.Cancelled => $"İptal edildi; {produced} konsept üretildi.",
            ConceptJobStatus.Completed when failed > 0 => $"{failed} konsept üretilemedi: {lastError}",
            ConceptJobStatus.Completed => null,
            _ => lastError ?? "Konsept üretilemedi.",
        };
        Services.Tracker.Update(job.Id, d => d with { Status = status, FinishedAt = finished, CurrentTitle = null, Message = message, CostUsd = cost });

        var text = string.Create(CultureInfo.InvariantCulture, $"{produced} konsept üretildi (${cost:0.00})");
        if (failed > 0)
        {
            text += $", {failed} başarısız";
        }

        if (cancelled)
        {
            text += ", iptal edildi";
        }

        var level = produced == 0 && !cancelled ? LogLevelKind.Error : failed > 0 ? LogLevelKind.Warning : LogLevelKind.Information;
        await Services.Activity.LogTimedAsync(
            ActivitySources.Concepts,
            text,
            started,
            finished,
            level,
            new { jobId = job.Id, kind = job.Kind.ToString(), produced, failed, costUsd = cost, lastError },
            job.UserId,
            CancellationToken.None);
        Services.Logger.LogInformation("Konsept işi bitti {JobId}: {Status}, {Produced} üretildi, {Failed} başarısız, ${Cost}", job.Id, status, produced, failed, cost);
    }
}
