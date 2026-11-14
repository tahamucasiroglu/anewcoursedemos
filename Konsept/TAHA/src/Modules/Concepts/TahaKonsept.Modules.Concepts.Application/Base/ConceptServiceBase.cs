using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Mapping;
using TahaKonsept.Modules.Concepts.Domain.Enums;
using TahaKonsept.Modules.Concepts.Domain.Errors;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Shared.Contracts.Concepts;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Concepts.Application.Base;

/// <summary>
/// <see cref="IConceptService"/> ortak davranışı (oturumdaki kullanıcıyla sınırlı). Düzenlemede dil/biçim/hedef süre mevcut belgeden
/// korunur. Silme → hafızada Deleted (negatif örnek); geri alma → Generated; kalıcı silme → hafıza izi silinir. Kullanıcıya görünen
/// işlemler <see cref="IActivityLogger"/>'a yazılır.
/// </summary>
public abstract class ConceptServiceBase : IConceptService
{
    private static readonly ConceptStatus[] ActiveStatuses = [ConceptStatus.Generated, ConceptStatus.Approved];

    /// <summary>Oluşturur.</summary>
    /// <param name="repository">Konsept deposu.</param>
    /// <param name="currentUser">Oturumdaki kullanıcı.</param>
    /// <param name="clock">Saat.</param>
    /// <param name="memory">Konu hafızası.</param>
    /// <param name="activity">İşlem günlüğü.</param>
    protected ConceptServiceBase(IConceptRepository repository, ICurrentUser currentUser, IClock clock, ITopicMemory memory, IActivityLogger activity)
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        CurrentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Memory = memory ?? throw new ArgumentNullException(nameof(memory));
        Activity = activity ?? throw new ArgumentNullException(nameof(activity));
    }

    /// <summary>Konsept deposu.</summary>
    protected IConceptRepository Repository { get; }

    /// <summary>Oturumdaki kullanıcı.</summary>
    protected ICurrentUser CurrentUser { get; }

    /// <summary>Saat.</summary>
    protected IClock Clock { get; }

    /// <summary>Konu hafızası.</summary>
    protected ITopicMemory Memory { get; }

    /// <summary>İşlem günlüğü.</summary>
    protected IActivityLogger Activity { get; }

    /// <summary>Liste.</summary>
    /// <param name="status">Durum.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Liste.</returns>
    public virtual async Task<Result<IReadOnlyList<ConceptSummaryDto>>> ListAsync(ConceptStatus? status, CancellationToken cancellationToken = default)
    {
        var statuses = status is { } s ? [s] : ActiveStatuses;
        var items = await Repository.ListForUserAsync(CurrentUser.GetRequiredUserId(), statuses, cancellationToken);
        return Result.Success<IReadOnlyList<ConceptSummaryDto>>(items.Select(c => c.ToSummary()).ToList());
    }

    /// <summary>Silinenler.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Liste.</returns>
    public virtual async Task<Result<IReadOnlyList<ConceptSummaryDto>>> ListDeletedAsync(CancellationToken cancellationToken = default)
    {
        var items = await Repository.ListForUserAsync(CurrentUser.GetRequiredUserId(), [ConceptStatus.Deleted], cancellationToken);
        return Result.Success<IReadOnlyList<ConceptSummaryDto>>(items
            .OrderByDescending(c => c.DeletedAtUtc ?? c.UpdatedAtUtc)
            .Select(c => c.ToSummary())
            .ToList());
    }

    /// <summary>Ayrıntı.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ayrıntı.</returns>
    public virtual async Task<Result<ConceptDetailDto>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var concept = await Repository.GetForUserAsync(id, CurrentUser.GetRequiredUserId(), cancellationToken);
        return concept is null ? ConceptErrors.NotFound : concept.ToDetail();
    }

    /// <summary>Doğrudan düzenleme.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ayrıntı.</returns>
    public virtual async Task<Result<ConceptDetailDto>> UpdateAsync(Guid id, UpdateConceptRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var concept = await Repository.GetForUserTrackedAsync(id, CurrentUser.GetRequiredUserId(), cancellationToken);
        if (concept is null)
        {
            return ConceptErrors.NotFound;
        }

        var current = concept.Document;
        var document = request.Document with
        {
            Language = current.Language,
            Format = current.Format,
            TargetDurationSec = current.TargetDurationSec,
        };
        var result = concept.UpdateDocument(document, request.Title);
        if (result.IsFailure)
        {
            return result.Error;
        }

        await Repository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return concept.ToDetail();
    }

    /// <summary>Silinenler'e taşır.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public virtual async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUser.GetRequiredUserId();
        var concept = await Repository.GetForUserTrackedAsync(id, userId, cancellationToken);
        if (concept is null)
        {
            return Result.Failure(ConceptErrors.NotFound);
        }

        concept.MarkDeleted(Clock.UtcNow);
        await Repository.UnitOfWork.SaveChangesAsync(cancellationToken);
        await Memory.MarkConceptAsync(userId, concept.ProfileId, concept.Id, concept.Title, TopicMemoryKind.Deleted, false, cancellationToken);
        await Activity.InformationAsync(ActivitySources.Concepts, $"Konsept silindi: {concept.Title}", userId: userId, cancellationToken: cancellationToken);
        return Result.Success();
    }

    /// <summary>Onaylar.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ayrıntı.</returns>
    public virtual async Task<Result<ConceptDetailDto>> ApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUser.GetRequiredUserId();
        var concept = await Repository.GetForUserTrackedAsync(id, userId, cancellationToken);
        if (concept is null)
        {
            return ConceptErrors.NotFound;
        }

        var result = concept.Approve(Clock.UtcNow);
        if (result.IsFailure)
        {
            return result.Error;
        }

        await Repository.UnitOfWork.SaveChangesAsync(cancellationToken);
        await Activity.InformationAsync(ActivitySources.Concepts, $"Konsept onaylandı: {concept.Title}", userId: userId, cancellationToken: cancellationToken);
        return concept.ToDetail();
    }

    /// <summary>Geri alır.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ayrıntı.</returns>
    public virtual async Task<Result<ConceptDetailDto>> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUser.GetRequiredUserId();
        var concept = await Repository.GetForUserTrackedAsync(id, userId, cancellationToken);
        if (concept is null)
        {
            return ConceptErrors.NotFound;
        }

        var result = concept.Restore();
        if (result.IsFailure)
        {
            return result.Error;
        }

        await Repository.UnitOfWork.SaveChangesAsync(cancellationToken);
        await Memory.MarkConceptAsync(userId, concept.ProfileId, concept.Id, concept.Title, TopicMemoryKind.Generated, true, cancellationToken);
        await Activity.InformationAsync(ActivitySources.Concepts, $"Konsept geri alındı: {concept.Title}", userId: userId, cancellationToken: cancellationToken);
        return concept.ToDetail();
    }

    /// <summary>Kalıcı siler.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public virtual async Task<Result> PurgeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUser.GetRequiredUserId();
        var concept = await Repository.GetForUserTrackedAsync(id, userId, cancellationToken);
        if (concept is null)
        {
            return Result.Failure(ConceptErrors.NotFound);
        }

        if (!concept.CanPurge)
        {
            return Result.Failure(ConceptErrors.NotDeleted);
        }

        var title = concept.Title;
        Repository.Remove(concept);
        await Repository.UnitOfWork.SaveChangesAsync(cancellationToken);
        await Memory.ForgetConceptAsync(userId, id, cancellationToken);
        await Activity.InformationAsync(ActivitySources.Concepts, $"Konsept kalıcı olarak silindi: {title}", userId: userId, cancellationToken: cancellationToken);
        return Result.Success();
    }
}
