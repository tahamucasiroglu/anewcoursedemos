using System.Globalization;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Domain.Enums;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Shared.Contracts.Concepts;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Concepts.Application.Base;

/// <summary>
/// <see cref="ITopicSuggestionService"/> ortak davranışı: bütçe kontrolü → bağlam (aktif profil) → hafıza → konu üreticisi →
/// önerileri hafızaya "Suggested" olarak yaz → işlem günlüğü. Beğenilmeyenler "Disliked" (negatif örnek) olur.
/// </summary>
public abstract class TopicSuggestionServiceBase : ITopicSuggestionService
{
    /// <summary>Oluşturur.</summary>
    /// <param name="currentUser">Oturumdaki kullanıcı.</param>
    /// <param name="budget">Bütçe bekçisi.</param>
    /// <param name="contexts">Bağlam kurucu.</param>
    /// <param name="memory">Konu hafızası.</param>
    /// <param name="generator">Konu üreticisi.</param>
    /// <param name="activity">İşlem günlüğü.</param>
    protected TopicSuggestionServiceBase(
        ICurrentUser currentUser,
        IBudgetGuard budget,
        IConceptContextProvider contexts,
        ITopicMemory memory,
        ITopicGenerator generator,
        IActivityLogger activity)
    {
        CurrentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        Budget = budget ?? throw new ArgumentNullException(nameof(budget));
        Contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));
        Memory = memory ?? throw new ArgumentNullException(nameof(memory));
        Generator = generator ?? throw new ArgumentNullException(nameof(generator));
        Activity = activity ?? throw new ArgumentNullException(nameof(activity));
    }

    /// <summary>Oturumdaki kullanıcı.</summary>
    protected ICurrentUser CurrentUser { get; }

    /// <summary>Bütçe bekçisi.</summary>
    protected IBudgetGuard Budget { get; }

    /// <summary>Bağlam kurucu.</summary>
    protected IConceptContextProvider Contexts { get; }

    /// <summary>Konu hafızası.</summary>
    protected ITopicMemory Memory { get; }

    /// <summary>Konu üreticisi.</summary>
    protected ITopicGenerator Generator { get; }

    /// <summary>İşlem günlüğü.</summary>
    protected IActivityLogger Activity { get; }

    /// <summary>Başlık önerir.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Öneriler.</returns>
    public virtual async Task<Result<TopicSuggestionsResponse>> SuggestAsync(SuggestTopicsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = CurrentUser.GetRequiredUserId();
        var allowed = await Budget.EnsureCanStartProductionAsync(userId, cancellationToken);
        if (allowed.IsFailure)
        {
            return allowed.Error;
        }

        var context = await Contexts.ResolveAsync(userId, null, cancellationToken);
        if (context.IsFailure)
        {
            return context.Error;
        }

        var snapshot = await Memory.LoadAsync(userId, context.Value.ProfileId, cancellationToken);
        var batch = await Generator.GenerateAsync(context.Value, request.Count, request.Hint, null, snapshot, cancellationToken);
        if (batch.IsFailure)
        {
            await Activity.WarningAsync(ActivitySources.Concepts, "Konu önerilemedi: " + batch.Error.Message, userId: userId, cancellationToken: cancellationToken);
            return batch.Error;
        }

        await Memory.RememberTitlesAsync(userId, context.Value.ProfileId, batch.Value.Items.Select(i => i.Title), TopicMemoryKind.Suggested, cancellationToken);
        await Activity.InformationAsync(
            ActivitySources.Concepts,
            string.Create(CultureInfo.InvariantCulture, $"{batch.Value.Items.Count} konu önerildi (${batch.Value.CostUsd:0.00})"),
            details: new { requested = request.Count, filteredOut = batch.Value.FilteredOut, request.Hint },
            userId: userId,
            cancellationToken: cancellationToken);

        return new TopicSuggestionsResponse
        {
            Items = batch.Value.Items,
            FilteredOut = batch.Value.FilteredOut,
            CostUsd = batch.Value.CostUsd,
        };
    }

    /// <summary>Beğenilmeyenleri kaydeder.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public virtual async Task<Result> DislikeAsync(DislikeTopicsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = CurrentUser.GetRequiredUserId();
        var context = await Contexts.ResolveAsync(userId, null, cancellationToken);
        if (context.IsFailure)
        {
            return Result.Failure(context.Error);
        }

        await Memory.RememberTitlesAsync(userId, context.Value.ProfileId, request.Titles, TopicMemoryKind.Disliked, cancellationToken);
        return Result.Success();
    }

    /// <summary>Hafızayı sıfırlar.</summary>
    /// <param name="all">Hepsi mi.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Silinen sayısı.</returns>
    public virtual async Task<Result<TopicMemoryResetResponse>> ResetMemoryAsync(bool all, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUser.GetRequiredUserId();
        var removed = await Memory.ResetAsync(userId, all, cancellationToken);
        await Activity.InformationAsync(ActivitySources.Concepts, $"Konu önerisi hafızası sıfırlandı ({removed} kayıt).", userId: userId, cancellationToken: cancellationToken);
        return new TopicMemoryResetResponse { Removed = removed };
    }
}
