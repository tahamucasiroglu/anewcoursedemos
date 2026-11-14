using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Models;
using TahaKonsept.Modules.Concepts.Domain.Constants;
using TahaKonsept.Modules.Concepts.Domain.Errors;
using TahaKonsept.Modules.Profiles.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Base;

/// <summary>
/// <see cref="IConceptGenerationService"/> ortak davranışı: <c>IBudgetGuard.EnsureCanStartProductionAsync</c> → aktif profil zorunlu →
/// iş izleyiciye kaydet → kuyruğa ekle → hemen dön (HTTP beklemez). Başlıklar kırpılır ve tekrarsızlaştırılır.
/// </summary>
public abstract class ConceptGenerationServiceBase : IConceptGenerationService
{
    /// <summary>Oluşturur.</summary>
    /// <param name="currentUser">Oturumdaki kullanıcı.</param>
    /// <param name="budget">Bütçe bekçisi.</param>
    /// <param name="profiles">Profil okuyucu.</param>
    /// <param name="queue">İş kuyruğu.</param>
    /// <param name="tracker">İş izleyicisi.</param>
    /// <param name="clock">Saat.</param>
    protected ConceptGenerationServiceBase(
        ICurrentUser currentUser,
        IBudgetGuard budget,
        IProfileReader profiles,
        IConceptJobQueue queue,
        IConceptJobTracker tracker,
        IClock clock)
    {
        CurrentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        Budget = budget ?? throw new ArgumentNullException(nameof(budget));
        Profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        Queue = queue ?? throw new ArgumentNullException(nameof(queue));
        Tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Oturumdaki kullanıcı.</summary>
    protected ICurrentUser CurrentUser { get; }

    /// <summary>Bütçe bekçisi.</summary>
    protected IBudgetGuard Budget { get; }

    /// <summary>Profil okuyucu.</summary>
    protected IProfileReader Profiles { get; }

    /// <summary>İş kuyruğu.</summary>
    protected IConceptJobQueue Queue { get; }

    /// <summary>İş izleyicisi.</summary>
    protected IConceptJobTracker Tracker { get; }

    /// <summary>Saat.</summary>
    protected IClock Clock { get; }

    /// <summary>Başlıklardan üretimi başlatır.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İş durumu.</returns>
    public virtual Task<Result<ConceptJobDto>> StartFromTopicsAsync(GenerateFromTopicsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var titles = request.Titles
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(ConceptLimits.MaxCount)
            .ToList();
        return StartAsync(ConceptJobKind.Topics, titles, null, titles.Count, cancellationToken);
    }

    /// <summary>Fikirden üretimi başlatır.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İş durumu.</returns>
    public virtual Task<Result<ConceptJobDto>> StartFromIdeaAsync(GenerateFromIdeaRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var count = Math.Clamp(request.Count, ConceptLimits.MinCount, ConceptLimits.MaxCount);
        return StartAsync(ConceptJobKind.Idea, [], request.Idea.Trim(), count, cancellationToken);
    }

    /// <summary>Güncel iş.</summary>
    /// <returns>Durum.</returns>
    public virtual Result<ConceptJobDto> GetCurrent() => Tracker.GetCurrent(CurrentUser.GetRequiredUserId());

    /// <summary>İşleri iptal eder.</summary>
    /// <returns>Durum.</returns>
    public virtual Result<ConceptJobDto> CancelCurrent() => Tracker.CancelAll(CurrentUser.GetRequiredUserId());

    private async Task<Result<ConceptJobDto>> StartAsync(ConceptJobKind kind, IReadOnlyList<string> titles, string? idea, int count, CancellationToken cancellationToken)
    {
        if (count <= 0)
        {
            return Error.Validation("concepts.empty_request", "Üretilecek en az bir başlık seçin.");
        }

        var userId = CurrentUser.GetRequiredUserId();
        var allowed = await Budget.EnsureCanStartProductionAsync(userId, cancellationToken);
        if (allowed.IsFailure)
        {
            return allowed.Error;
        }

        var profile = await Profiles.GetActiveAsync(userId, cancellationToken);
        if (profile is null)
        {
            return ConceptErrors.NoProfile;
        }

        var job = new ConceptJob
        {
            UserId = userId,
            ProfileId = profile.Id,
            Kind = kind,
            Titles = titles,
            Idea = idea,
            Count = count,
            CreatedAtUtc = Clock.UtcNow,
        };
        Tracker.Register(job);
        await Queue.EnqueueAsync(job, cancellationToken);
        return Tracker.GetCurrent(userId);
    }
}
