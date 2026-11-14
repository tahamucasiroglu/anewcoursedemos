using System.Globalization;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Mapping;
using TahaKonsept.Modules.Concepts.Application.Models;
using TahaKonsept.Modules.Concepts.Domain.Enums;
using TahaKonsept.Modules.Concepts.Domain.Errors;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Shared.Contracts.Concepts;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Concepts.Application.Base;

/// <summary>
/// <see cref="IConceptRevisionService"/> ortak davranışı. Düzelt: bütçe → bağlam (konseptin profili; dil/biçim/süre belgeden) →
/// temel belge (istekteki ya da kayıtlı) → LLM düzeltme → (açıksa) kelime denetimi → aday deposu. Kabul: belge değişir, maliyet eklenir,
/// konseptin diğer adayları atılır. At: aday silinir, harcanan maliyet yine konsepte yazılır.
/// </summary>
public abstract class ConceptRevisionServiceBase : IConceptRevisionService
{
    /// <summary>Oluşturur.</summary>
    /// <param name="repository">Konsept deposu.</param>
    /// <param name="currentUser">Oturumdaki kullanıcı.</param>
    /// <param name="budget">Bütçe bekçisi.</param>
    /// <param name="contexts">Bağlam kurucu.</param>
    /// <param name="generator">Konsept üreticisi.</param>
    /// <param name="wordCheck">Kelime denetimi.</param>
    /// <param name="candidates">Aday deposu.</param>
    /// <param name="memory">Konu hafızası.</param>
    /// <param name="clock">Saat.</param>
    /// <param name="activity">İşlem günlüğü.</param>
    protected ConceptRevisionServiceBase(
        IConceptRepository repository,
        ICurrentUser currentUser,
        IBudgetGuard budget,
        IConceptContextProvider contexts,
        IConceptGenerator generator,
        IWordRepetitionChecker wordCheck,
        IRevisionCandidateStore candidates,
        ITopicMemory memory,
        IClock clock,
        IActivityLogger activity)
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        CurrentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        Budget = budget ?? throw new ArgumentNullException(nameof(budget));
        Contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));
        Generator = generator ?? throw new ArgumentNullException(nameof(generator));
        WordCheck = wordCheck ?? throw new ArgumentNullException(nameof(wordCheck));
        Candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
        Memory = memory ?? throw new ArgumentNullException(nameof(memory));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Activity = activity ?? throw new ArgumentNullException(nameof(activity));
    }

    /// <summary>Konsept deposu.</summary>
    protected IConceptRepository Repository { get; }

    /// <summary>Oturumdaki kullanıcı.</summary>
    protected ICurrentUser CurrentUser { get; }

    /// <summary>Bütçe bekçisi.</summary>
    protected IBudgetGuard Budget { get; }

    /// <summary>Bağlam kurucu.</summary>
    protected IConceptContextProvider Contexts { get; }

    /// <summary>Konsept üreticisi.</summary>
    protected IConceptGenerator Generator { get; }

    /// <summary>Kelime denetimi.</summary>
    protected IWordRepetitionChecker WordCheck { get; }

    /// <summary>Aday deposu.</summary>
    protected IRevisionCandidateStore Candidates { get; }

    /// <summary>Konu hafızası.</summary>
    protected ITopicMemory Memory { get; }

    /// <summary>Saat.</summary>
    protected IClock Clock { get; }

    /// <summary>İşlem günlüğü.</summary>
    protected IActivityLogger Activity { get; }

    /// <summary>Düzeltme adayı üretir.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Aday.</returns>
    public virtual async Task<Result<ConceptRevisionCandidateDto>> ReviseAsync(Guid id, ReviseConceptRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = CurrentUser.GetRequiredUserId();
        var concept = await Repository.GetForUserAsync(id, userId, cancellationToken);
        if (concept is null)
        {
            return ConceptErrors.NotFound;
        }

        if (concept.Status == ConceptStatus.Deleted)
        {
            return ConceptErrors.IsDeleted;
        }

        var allowed = await Budget.EnsureCanStartProductionAsync(userId, cancellationToken);
        if (allowed.IsFailure)
        {
            return allowed.Error;
        }

        var contextResult = await Contexts.ResolveAsync(userId, concept.ProfileId, cancellationToken);
        if (contextResult.IsFailure)
        {
            return contextResult.Error;
        }

        var saved = concept.Document;
        var baseDocument = (request.Document ?? saved) with
        {
            Language = saved.Language,
            Format = saved.Format,
            TargetDurationSec = saved.TargetDurationSec,
        };
        var context = contextResult.Value with
        {
            Language = saved.Language,
            Format = saved.Format,
            TargetDurationSec = saved.TargetDurationSec,
        };

        var revised = await Generator.ReviseAsync(context, baseDocument, request.Instruction, cancellationToken);
        if (revised.IsFailure)
        {
            return revised.Error;
        }

        var document = revised.Value.Document;
        long input = revised.Value.InputTokens, output = revised.Value.OutputTokens;
        var cost = revised.Value.CostUsd;
        if (context.Preferences.WordRepetitionCheck)
        {
            var checkedDoc = await WordCheck.CheckAsync(context, document, cancellationToken);
            if (checkedDoc.IsSuccess)
            {
                document = checkedDoc.Value.Document;
                input += checkedDoc.Value.InputTokens;
                output += checkedDoc.Value.OutputTokens;
                cost += checkedDoc.Value.CostUsd;
            }
        }

        var candidate = new RevisionCandidate(Guid.CreateVersion7(), concept.Id, userId, request.Instruction.Trim(), document, input, output, cost, Clock.UtcNow);
        Candidates.Add(candidate);
        await Activity.InformationAsync(
            ActivitySources.Concepts,
            string.Create(CultureInfo.InvariantCulture, $"Konsept düzeltildi (aday): {document.Title} (${cost:0.00})"),
            details: new { conceptId = concept.Id, instruction = candidate.Instruction },
            durationMs: revised.Value.DurationMs,
            userId: userId,
            cancellationToken: cancellationToken);

        return new ConceptRevisionCandidateDto
        {
            CandidateId = candidate.CandidateId,
            ConceptId = concept.Id,
            Instruction = candidate.Instruction,
            Document = document,
            CostUsd = cost,
            CreatedAt = candidate.CreatedAtUtc,
        };
    }

    /// <summary>Adayı kabul eder.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="candidateId">Aday.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncel konsept.</returns>
    public virtual async Task<Result<ConceptDetailDto>> AcceptAsync(Guid id, Guid candidateId, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUser.GetRequiredUserId();
        var concept = await Repository.GetForUserTrackedAsync(id, userId, cancellationToken);
        if (concept is null)
        {
            return ConceptErrors.NotFound;
        }

        var candidate = Candidates.Find(userId, id, candidateId);
        if (candidate is null)
        {
            return ConceptErrors.CandidateNotFound;
        }

        var result = concept.AcceptRevision(candidate.Document, candidate.InputTokens, candidate.OutputTokens, candidate.CostUsd);
        if (result.IsFailure)
        {
            return result.Error;
        }

        var others = Candidates.RemoveForConcept(id).Where(c => c.CandidateId != candidateId).ToList();
        foreach (var other in others)
        {
            concept.AddUsage(other.InputTokens, other.OutputTokens, other.CostUsd);
        }

        await Repository.UnitOfWork.SaveChangesAsync(cancellationToken);
        await Memory.MarkConceptAsync(userId, concept.ProfileId, concept.Id, concept.Title, TopicMemoryKind.Generated, false, cancellationToken);
        await Activity.InformationAsync(ActivitySources.Concepts, $"Düzeltilmiş sürüm seçildi: {concept.Title}", userId: userId, cancellationToken: cancellationToken);
        return concept.ToDetail();
    }

    /// <summary>Adayı atar.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="candidateId">Aday.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public virtual async Task<Result> DiscardAsync(Guid id, Guid candidateId, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUser.GetRequiredUserId();
        var candidate = Candidates.Find(userId, id, candidateId);
        if (candidate is null)
        {
            return Result.Failure(ConceptErrors.CandidateNotFound);
        }

        Candidates.Remove(candidateId);
        var concept = await Repository.GetForUserTrackedAsync(id, userId, cancellationToken);
        if (concept is not null)
        {
            concept.AddUsage(candidate.InputTokens, candidate.OutputTokens, candidate.CostUsd);
            await Repository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
