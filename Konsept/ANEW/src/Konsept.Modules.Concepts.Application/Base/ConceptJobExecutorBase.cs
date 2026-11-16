using System.Globalization;
using Konsept.Modules.AiGateway.Contracts.Abstract;
using Konsept.Modules.AiGateway.Contracts.Dtos;
using Konsept.Modules.AiGateway.Contracts.Enums;
using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Application.Dtos;
using Konsept.Modules.Concepts.Application.Resources;
using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Enums;
using Konsept.Modules.Concepts.Domain.Rules;
using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Contracts.Enums;
using Konsept.Shared.Kernel.Results;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Concepts.Application.Base;

/// <summary>
/// Üretim işinin yürütücüsü (spec 0014, H-3). Kurallar:
/// <list type="bullet">
/// <item>Her model çağrısı <see cref="IModelCaller"/> ile 0013 kapısından, kendi rolü ve profille geçer (harcama kaydı, BR-12); bu sınıf
/// hiçbir çağrıyı kendisi tekrar etmez (Req 17).</item>
/// <item>Konu üret: tek öneri çağrısı; biçim dışı (Req 3) ve geçmişle birebir aynı (Req 4) adaylar elenir, eksik adet için yeni çağrı
/// yok; kalan adaylar için tek anlamca denetim çağrısı (Req 6) — başarısızsa yalnızca birebir eleme.</item>
/// <item>Konsept: tek çağrı (G-18); yarım kalan ya da ayrıştırılamayan çıktı kaydedilmez ve otomatik tekrar edilmez (Req 18). Kelime
/// denetimi açıkken eşik aşılırsa en fazla 1 yeniden üretim (BR-15); süre uyumsuzsa uyarıyla kaydedilir, ek çağrı yok (Req 11).</item>
/// <item>Konseptler sırayla üretilir; Durdur sıradakileri başlatmaz, süren çağrı tamamlanır (Req 19).</item>
/// <item>Fikirden üretimde birebir tekrarlar anında, anlamca tekrarlar parti sonunda tek denetim çağrısıyla elenir; yerine üretim yok (Req 9).</item>
/// <item>İş bitince (bitti/durduruldu/başarısız) Günlük'e başlık içermeyen bir olay yazılır (Req 21).</item>
/// </list>
/// </summary>
/// <param name="caller">Model çağrısı (AiGateway.Contracts).</param>
/// <param name="concepts">Konsept deposu.</param>
/// <param name="suggestions">Öneri deposu.</param>
/// <param name="rules">Kurallar.</param>
/// <param name="events">Günlük olay yazıcısı (Monitoring.Contracts).</param>
/// <param name="time">Saat.</param>
/// <param name="logger">Log.</param>
public abstract class ConceptJobExecutorBase(
    IModelCaller caller,
    IConceptRepository concepts,
    ITopicSuggestionRepository suggestions,
    IConceptRuleSource rules,
    ISystemEventWriter events,
    TimeProvider time,
    ILogger logger) : IConceptJobExecutor
{
    /// <summary>Günlük olay türü: konu önerisi.</summary>
    public const string TopicsEventType = "concepts.topics";

    /// <summary>Günlük olay türü: konsept üretimi.</summary>
    public const string ConceptsEventType = "concepts.generation";

    private static readonly Action<ILogger, Guid, Exception?> JobCrashed = LoggerMessage.Define<Guid>(
        LogLevel.Error, new EventId(5101, "ConceptJobCrashed"), "Konsept üretim işi beklenmeyen hatayla durdu: {JobId}");

    private static readonly Action<ILogger, Guid, Exception?> EventFailed = LoggerMessage.Define<Guid>(
        LogLevel.Warning, new EventId(5102, "ConceptJobEventFailed"), "Konsept üretim işinin Günlük olayı yazılamadı: {JobId}");

    /// <inheritdoc />
    public async Task ExecuteAsync(ConceptJob job, ConceptJobInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(input);
        try
        {
            switch (job.Kind)
            {
                case JobKind.Topics:
                    await RunTopicsAsync(job, input, cancellationToken).ConfigureAwait(false);
                    break;
                case JobKind.FromTopics:
                    await RunFromTopicsAsync(job, input, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    await RunFromIdeaAsync(job, input, cancellationToken).ConfigureAwait(false);
                    break;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            job.RequestStop(); // Uygulama kapanıyor: iş durdurulmuş sayılır.
            job.Finish();
        }
        catch (Exception ex)
        {
            JobCrashed(logger, job.Id, ex);
            job.Finish(ConceptsText.Unexpected);
        }

        await WriteEventAsync(job, input).ConfigureAwait(false);
    }

    private static FineTuneChoice FineTune(ConceptJobInput input) =>
        new(input.Context.Tone, input.Context.Audience, input.Context.Narration);

    private async Task RunTopicsAsync(ConceptJob job, ConceptJobInput input, CancellationToken cancellationToken)
    {
        var context = input.Context;
        var set = rules.Rules;
        var history = await HistoryAsync(input, cancellationToken).ConfigureAwait(false);
        var view = TopicHistory.Build(history, context.Language, set);
        var reply = await CallAsync(
            input,
            context.TopicModel!,
            context.TopicEffort,
            ModelRole.TopicSuggestion,
            ConceptPrompts.TopicSystem(context.Instruction, FineTune(input), view),
            ConceptPrompts.TopicPrompt(job.Total),
            cancellationToken).ConfigureAwait(false);
        if (!reply.IsSuccess)
        {
            job.Finish(reply.Error!.Message);
            return;
        }

        if (reply.Value.Truncated)
        {
            job.Finish(ConceptsText.TopicsTruncated);
            return;
        }

        if (ModelOutputParser.Topics(reply.Value.Text) is not { } parsed)
        {
            job.Finish(ConceptsText.TopicsMalformed);
            return;
        }

        var known = history.Select(h => TitleKey.Of(h.Title, context.Language)).ToHashSet(StringComparer.Ordinal);
        var formatRejected = 0;
        var duplicates = 0;
        var candidates = new List<ParsedTopic>();
        foreach (var topic in parsed.Take(job.Total))
        {
            if (!SuggestionFormat.IsValid(topic))
            {
                formatRejected++; // Kesilmez, listelenmez; eksik adet için yeni çağrı yok (Req 3).
            }
            else if (!known.Add(TitleKey.Of(topic.Title, context.Language)))
            {
                duplicates++; // Geçmişte (ya da bu partide) birebir aynı başlık (BR-4).
            }
            else
            {
                candidates.Add(topic);
            }
        }

        var (flagged, unavailable) = await CheckDuplicatesAsync(input, candidates.Select(c => c.Title).ToList(), view, cancellationToken).ConfigureAwait(false);
        var now = time.GetUtcNow();
        var kept = candidates.Where((_, i) => !flagged.Contains(i + 1)).ToList();
        await suggestions.AddRangeAsync(
            [.. kept.Select((t, i) => new TopicSuggestion
            {
                Id = Guid.CreateVersion7(),
                UserId = input.Caller.UserId,
                ProfileId = context.ProfileId,
                Title = t.Title,
                TitleKey = TitleKey.Of(t.Title, context.Language),
                Summary = t.Summary,
                State = SuggestionState.Pending,
                CreatedAt = now.AddTicks(i), // Model sırası korunur.
            })],
            CancellationToken.None).ConfigureAwait(false);
        job.SetTopicResult(kept.Count, formatRejected, duplicates + (candidates.Count - kept.Count), unavailable);
        job.Finish();
    }

    private async Task RunFromTopicsAsync(ConceptJob job, ConceptJobInput input, CancellationToken cancellationToken)
    {
        // Konsepte çevrilemeyen (durdurulan ya da hata veren) öneriler listeye geri döner; kullanıcı isterse elle yeniden üretir
        // (otomatik tekrar yok, review r1 M-4).
        // Kaydedilmeyen = tümü − kaydedilenler: istisnada da (review r2) süren ve sonraki öneriler geri döner.
        var saved = new HashSet<Guid>();
        try
        {
            foreach (var topic in input.Topics)
            {
                if (job.StopRequested)
                {
                    continue; // Sıradakiler başlamaz (G-3).
                }

                var outcome = await GenerateAsync(
                    input,
                    avoid => ConceptPrompts.FromTopicPrompt(topic.Title, topic.Summary, avoid),
                    cancellationToken).ConfigureAwait(false);
                if (outcome.Error is { } error)
                {
                    job.Fail(topic.Title, error);
                    continue;
                }

                // Başlık seçilen önerininkidir: geçmiş ve "seçilen konular" bu başlıkla tutarlı kalır (BR-4).
                await concepts.AddAsync(Build(input, topic.Title, outcome, ConceptSource.Topic), CancellationToken.None).ConfigureAwait(false);
                saved.Add(topic.Id);
                job.Succeed();
            }
        }
        finally
        {
            var unsaved = input.Topics.Select(t => t.Id).Where(id => !saved.Contains(id)).ToList();
            if (unsaved.Count > 0)
            {
                await suggestions.RestoreSelectedAsync(input.Caller.UserId, input.Context.ProfileId, unsaved, CancellationToken.None).ConfigureAwait(false);
            }
        }

        job.Finish();
    }

    private async Task RunFromIdeaAsync(ConceptJob job, ConceptJobInput input, CancellationToken cancellationToken)
    {
        var context = input.Context;
        var history = await HistoryAsync(input, cancellationToken).ConfigureAwait(false);
        var known = history.Select(h => TitleKey.Of(h.Title, context.Language)).ToHashSet(StringComparer.Ordinal);
        var titles = new List<string>();
        var pending = new List<Concept>();
        var duplicates = 0;
        for (var index = 1; index <= job.Total; index++)
        {
            if (job.StopRequested)
            {
                break;
            }

            var outcome = await GenerateAsync(
                input,
                avoid => ConceptPrompts.FromIdeaPrompt(input.Idea!, titles, avoid),
                cancellationToken).ConfigureAwait(false);
            if (outcome.Error is { } error)
            {
                job.Fail(index.ToString(CultureInfo.InvariantCulture), error);
                continue;
            }

            var title = outcome.Concept!.Title;
            titles.Add(title);
            job.Advance();
            if (!known.Add(TitleKey.Of(title, context.Language)))
            {
                duplicates++; // Partide ya da profilde birebir aynı: kaydedilmez, yerine üretim yok (G-6, H-3).
                continue;
            }

            pending.Add(Build(input, title, outcome, ConceptSource.Idea));
        }

        var view = TopicHistory.Build(history, context.Language, rules.Rules);
        var (flagged, unavailable) = await CheckDuplicatesAsync(input, pending.Select(c => c.Title).ToList(), view, cancellationToken).ConfigureAwait(false);
        var kept = pending.Where((_, i) => !flagged.Contains(i + 1)).ToList();
        foreach (var concept in kept)
        {
            await concepts.AddAsync(concept, CancellationToken.None).ConfigureAwait(false);
        }

        job.AddSaved(kept.Count);
        job.AddDuplicates(duplicates + (pending.Count - kept.Count), unavailable);
        job.Finish();
    }

    // Tek anlamca tekrar denetimi çağrısı (S-4): aday yoksa çağrı yok; başarısızsa ek çağrı yok, yalnızca birebir eleme kalır.
    private async Task<(IReadOnlySet<int> Flagged, bool Unavailable)> CheckDuplicatesAsync(
        ConceptJobInput input, IReadOnlyList<string> candidates, TopicHistoryView view, CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            return (new HashSet<int>(), false);
        }

        var context = input.Context;
        var reply = await CallAsync(
            input,
            context.DedupeModel!,
            context.DedupeEffort,
            ModelRole.DuplicateCheck,
            ConceptPrompts.DuplicateSystem(),
            ConceptPrompts.DuplicatePrompt(candidates, view.Verbatim.Select(t => t.Title)),
            cancellationToken).ConfigureAwait(false);
        return reply.IsSuccess && !reply.Value.Truncated && ModelOutputParser.Duplicates(reply.Value.Text) is { } flagged
            ? (flagged, false)
            : (new HashSet<int>(), true);
    }

    private async Task<GeneratedConcept> GenerateAsync(ConceptJobInput input, Func<bool, string> prompt, CancellationToken cancellationToken)
    {
        var context = input.Context;
        var system = ConceptPrompts.ConceptSystem(context.Instruction, FineTune(input), context.DurationSeconds);
        var first = await GenerateOnceAsync(input, system, prompt(false), cancellationToken).ConfigureAwait(false);
        if (first.Error is not null || !context.WordCheck || !Repeats(first.Concept!, context.Language))
        {
            return first;
        }

        // BR-15: en fazla 1 yeniden üretim; yine aşılırsa (ya da ikinci çıktı kullanılamazsa) ilk konsept uyarıyla kaydedilir.
        var second = await GenerateOnceAsync(input, system, prompt(true), cancellationToken).ConfigureAwait(false);
        return second.Error is null
            ? second with { WordRepetition = Repeats(second.Concept!, context.Language) }
            : first with { WordRepetition = true };
    }

    private async Task<GeneratedConcept> GenerateOnceAsync(ConceptJobInput input, string system, string prompt, CancellationToken cancellationToken)
    {
        var context = input.Context;
        var reply = await CallAsync(input, context.ConceptModel!, context.ConceptEffort, ModelRole.ConceptGeneration, system, prompt, cancellationToken)
            .ConfigureAwait(false);
        if (!reply.IsSuccess)
        {
            return new GeneratedConcept(null, false, reply.Error!.Message);
        }

        if (reply.Value.Truncated)
        {
            return new GeneratedConcept(null, false, ConceptsText.ConceptTruncated);
        }

        return ModelOutputParser.Concept(reply.Value.Text) is { } parsed
            ? new GeneratedConcept(parsed, false, null)
            : new GeneratedConcept(null, false, ConceptsText.ConceptMalformed);
    }

    private bool Repeats(ParsedConcept concept, string language) =>
        WordRepetition.Exceeds(concept.Scenes.Select(s => s.Narration), language, rules.Rules);

    private Task<Result<TextModelReply>> CallAsync(
        ConceptJobInput input, string model, string? effort, ModelRole role, string system, string prompt, CancellationToken cancellationToken) =>
        caller.CallTextAsync(
            new TextModelCall
            {
                Caller = input.Caller,
                ModelId = model,
                Role = role,
                ProfileId = input.Context.ProfileId,
                Effort = effort,
                System = system,
                Prompt = prompt,
            },
            cancellationToken);

    private Concept Build(ConceptJobInput input, string title, GeneratedConcept outcome, ConceptSource source)
    {
        var context = input.Context;
        var scenes = outcome.Concept!.Scenes;
        var now = time.GetUtcNow();
        return new Concept
        {
            Id = Guid.CreateVersion7(),
            UserId = input.Caller.UserId,
            ProfileId = context.ProfileId,
            Title = title,
            TitleKey = TitleKey.Of(title, context.Language),
            Source = source,
            Status = ConceptStatus.Generated,
            Language = context.Language,
            Format = context.Format,
            TargetDurationSeconds = context.DurationSeconds,
            Scenes = scenes,
            WordRepetitionWarning = outcome.WordRepetition,
            DurationMismatchWarning = DurationFit.IsMismatch(scenes.Sum(s => s.DurationSeconds), context.DurationSeconds, rules.Rules.DurationTolerance),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    // Profil geçmişi: tüm konsept başlıkları + bekleyen dışındaki öneriler (seçilmeyenler "elenen", seçilenler "seçilen").
    private async Task<IReadOnlyList<HistoryTitle>> HistoryAsync(ConceptJobInput input, CancellationToken cancellationToken)
    {
        var userId = input.Caller.UserId;
        var profileId = input.Context.ProfileId;
        var titles = new List<HistoryTitle>(await concepts.ListTitlesAsync(userId, profileId, cancellationToken).ConfigureAwait(false));
        var rows = await suggestions.ListAsync(userId, profileId, cancellationToken).ConfigureAwait(false);
        titles.AddRange(rows
            .Where(s => s.State != SuggestionState.Pending)
            .Select(s => new HistoryTitle(s.Title, s.State == SuggestionState.Selected, s.CreatedAt)));
        return titles;
    }

    private async Task WriteEventAsync(ConceptJob job, ConceptJobInput input)
    {
        var s = job.Snapshot();
        var culture = CultureInfo.InvariantCulture;
        var (type, level, message) = (s.Kind, s.Status) switch
        {
            (JobKind.Topics, JobStatus.Failed) => (TopicsEventType, SystemEventLevel.Warning, ConceptsText.EventTopicsFailed),
            (JobKind.Topics, _) => (TopicsEventType, SystemEventLevel.Info, string.Format(culture, ConceptsText.EventTopics, s.Succeeded)),
            (_, JobStatus.Failed) => (ConceptsEventType, SystemEventLevel.Error, string.Format(culture, ConceptsText.EventConceptsFailed, s.Total)),
            (_, JobStatus.Stopped) => (ConceptsEventType, SystemEventLevel.Info, string.Format(culture, ConceptsText.EventConceptsStopped, s.Succeeded, s.Total)),
            _ when s.Failures.Count > 0 => (ConceptsEventType, SystemEventLevel.Warning, string.Format(culture, ConceptsText.EventConceptsWithErrors, s.Succeeded, s.Total, s.Failures.Count)),
            _ => (ConceptsEventType, SystemEventLevel.Info, string.Format(culture, ConceptsText.EventConcepts, s.Succeeded, s.Total)),
        };
        try
        {
            await events.WriteAsync(
                new SystemEventEntry(type, level, message, job.UserId, job.StartedAt, time.GetUtcNow(), job.ProfileId, input.Context.Name),
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            EventFailed(logger, job.Id, ex); // Günlük yazılamasa da üretim sonucu kaybolmaz.
        }
    }
}
