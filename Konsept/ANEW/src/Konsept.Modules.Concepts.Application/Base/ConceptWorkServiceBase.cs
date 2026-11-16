using System.Text.Json;
using Konsept.Modules.AiGateway.Contracts.Abstract;
using Konsept.Modules.AiGateway.Contracts.Dtos;
using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Application.Dtos;
using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Enums;
using Konsept.Modules.Concepts.Domain.Rules;
using Konsept.Modules.Profiles.Contracts.Abstract;
using Konsept.Modules.Profiles.Contracts.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Concepts.Application.Base;

/// <summary>
/// Konseptler sayfasının üretim alanları (spec 0014 Req 1–9, 19–20). Sıra: oturum → profil kullanıcının mı (değilse "bulunamadı",
/// BR-1) → alan kuralları (adet 1–100 tam sayı, fikir 1–2.000 karakter, seçim) → hazır olma (rol için seçilmiş ve seçilebilir model —
/// yetki + geçerli key, BR-13) → şerit boş mu. Reddedilen istekte hiçbir online çağrı yapılmaz. Kabul edilen istekte çağıranın kimliği
/// ve profilin o anki değerleri yakalanır, iş arka planda başlar ve istek hemen döner (Req 20).
/// </summary>
/// <param name="caller">Model çağrısı (çağıran kimliği).</param>
/// <param name="profiles">Profil bağlamı (Profiles.Contracts).</param>
/// <param name="models">Seçilebilir metin modelleri (AiGateway.Contracts).</param>
/// <param name="suggestions">Öneri deposu.</param>
/// <param name="registry">İş kaydı.</param>
/// <param name="launcher">Arka plan başlatıcı.</param>
/// <param name="time">Saat.</param>
public abstract class ConceptWorkServiceBase(
    IModelCaller caller,
    IProfileGenerationReader profiles,
    ITextModelDirectory models,
    ITopicSuggestionRepository suggestions,
    IConceptJobRegistry registry,
    IConceptJobLauncher launcher,
    TimeProvider time) : IConceptWorkService
{
    /// <inheritdoc />
    public async Task<Result<ConceptWorkDto>> GetAsync(Guid profileId, CancellationToken cancellationToken)
    {
        var scope = await ScopeAsync(profileId, cancellationToken).ConfigureAwait(false);
        return scope.IsSuccess
            ? Result<ConceptWorkDto>.Success(await WorkAsync(scope.Value.Caller, scope.Value.Context, cancellationToken).ConfigureAwait(false))
            : Result<ConceptWorkDto>.Failure(scope.Error!);
    }

    /// <inheritdoc />
    public async Task<Result<ConceptWorkDto>> StartTopicsAsync(StartTopicsRequest? request, CancellationToken cancellationToken)
    {
        var scope = await ScopeAsync(request?.ProfileId, cancellationToken).ConfigureAwait(false);
        if (!scope.IsSuccess)
        {
            return Result<ConceptWorkDto>.Failure(scope.Error!);
        }

        if (ParseCount(request!.Count) is not { } count)
        {
            return Result<ConceptWorkDto>.Failure(ConceptsErrors.CountInvalid);
        }

        var (who, context) = scope.Value;
        var readiness = await ReadinessAsync(context, cancellationToken).ConfigureAwait(false);
        if (!readiness.Topics)
        {
            return Result<ConceptWorkDto>.Failure(ConceptsErrors.NotReady);
        }

        var job = NewJob(who, context, JobLane.Topic, JobKind.Topics, count);
        if (!registry.TryStart(job))
        {
            return Result<ConceptWorkDto>.Failure(ConceptsErrors.Busy);
        }

        // Yeni "Konu üret": bekleyen tüm öneriler "önerilip seçilmemiş" olur ve listeden kalkar (Req 7).
        await PrepareAsync(job, () => suggestions.SetPendingStateAsync(who.UserId, context.ProfileId, null, SuggestionState.Rejected, CancellationToken.None))
            .ConfigureAwait(false);
        launcher.Launch(job, new ConceptJobInput(who, context, count, null, []));
        return Result<ConceptWorkDto>.Success(await WorkAsync(who, context, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<Result<ConceptWorkDto>> StartFromTopicsAsync(StartFromTopicsRequest? request, CancellationToken cancellationToken)
    {
        var scope = await ScopeAsync(request?.ProfileId, cancellationToken).ConfigureAwait(false);
        if (!scope.IsSuccess)
        {
            return Result<ConceptWorkDto>.Failure(scope.Error!);
        }

        var ids = request!.SuggestionIds?.Distinct().ToList() ?? [];
        if (ids.Count == 0)
        {
            return Result<ConceptWorkDto>.Failure(ConceptsErrors.SelectionRequired);
        }

        var (who, context) = scope.Value;
        var pending = (await suggestions.ListAsync(who.UserId, context.ProfileId, cancellationToken).ConfigureAwait(false))
            .Where(s => s.State == SuggestionState.Pending)
            .ToList();
        var selected = pending.Where(s => ids.Contains(s.Id)).ToList();
        if (selected.Count != ids.Count)
        {
            return Result<ConceptWorkDto>.Failure(ConceptsErrors.NotFound); // Başkasının ya da bekleyen olmayan öneri (BR-1).
        }

        var readiness = await ReadinessAsync(context, cancellationToken).ConfigureAwait(false);
        if (!readiness.FromTopics)
        {
            return Result<ConceptWorkDto>.Failure(ConceptsErrors.NotReady);
        }

        var job = NewJob(who, context, JobLane.Topic, JobKind.FromTopics, selected.Count);
        if (!registry.TryStart(job))
        {
            return Result<ConceptWorkDto>.Failure(ConceptsErrors.Busy);
        }

        // Seçilenler üretime gider; listedeki seçilmeyenler "önerilip seçilmemiş" olur ve listeden kalkar (Req 7).
        await PrepareAsync(job, async () =>
        {
            await suggestions.SetPendingStateAsync(who.UserId, context.ProfileId, [.. selected.Select(s => s.Id)], SuggestionState.Selected, CancellationToken.None)
                .ConfigureAwait(false);
            await suggestions.SetPendingStateAsync(who.UserId, context.ProfileId, null, SuggestionState.Rejected, CancellationToken.None).ConfigureAwait(false);
        }).ConfigureAwait(false);
        launcher.Launch(job, new ConceptJobInput(who, context, selected.Count, null, selected));
        return Result<ConceptWorkDto>.Success(await WorkAsync(who, context, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<Result<ConceptWorkDto>> StartFromIdeaAsync(StartFromIdeaRequest? request, CancellationToken cancellationToken)
    {
        var scope = await ScopeAsync(request?.ProfileId, cancellationToken).ConfigureAwait(false);
        if (!scope.IsSuccess)
        {
            return Result<ConceptWorkDto>.Failure(scope.Error!);
        }

        var idea = request!.Idea?.Trim() ?? string.Empty;
        if (idea.Length == 0)
        {
            return Result<ConceptWorkDto>.Failure(ConceptsErrors.IdeaRequired);
        }

        if (idea.Length > ConceptLimits.MaxIdeaLength)
        {
            return Result<ConceptWorkDto>.Failure(ConceptsErrors.IdeaTooLong);
        }

        if (ParseCount(request.Count) is not { } count)
        {
            return Result<ConceptWorkDto>.Failure(ConceptsErrors.CountInvalid);
        }

        var (who, context) = scope.Value;
        var readiness = await ReadinessAsync(context, cancellationToken).ConfigureAwait(false);
        if (!readiness.Idea)
        {
            return Result<ConceptWorkDto>.Failure(ConceptsErrors.NotReady);
        }

        var job = NewJob(who, context, JobLane.Idea, JobKind.FromIdea, count);
        if (!registry.TryStart(job))
        {
            return Result<ConceptWorkDto>.Failure(ConceptsErrors.Busy);
        }

        launcher.Launch(job, new ConceptJobInput(who, context, count, idea, []));
        return Result<ConceptWorkDto>.Success(await WorkAsync(who, context, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<Result<ConceptWorkDto>> StopAsync(StopJobRequest? request, CancellationToken cancellationToken)
    {
        var scope = await ScopeAsync(request?.ProfileId, cancellationToken).ConfigureAwait(false);
        if (!scope.IsSuccess)
        {
            return Result<ConceptWorkDto>.Failure(scope.Error!);
        }

        JobLane? lane = request!.Lane switch
        {
            "topic" => JobLane.Topic,
            "idea" => JobLane.Idea,
            _ => null,
        };
        if (lane is null)
        {
            return Result<ConceptWorkDto>.Failure(ConceptsErrors.LaneInvalid);
        }

        var (who, context) = scope.Value;
        if (registry.Get(who.UserId, lane.Value) is { } job && job.ProfileId == context.ProfileId)
        {
            job.RequestStop();
        }

        return Result<ConceptWorkDto>.Success(await WorkAsync(who, context, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Adet: yalnızca 1–100 arası JSON tam sayısı (ondalık, metin, boş reddedilir; AC-2).</summary>
    /// <param name="value">Ham değer.</param>
    /// <returns>Adet ya da <c>null</c>.</returns>
    protected static int? ParseCount(JsonElement? value) =>
        value is { ValueKind: JsonValueKind.Number } number
            && number.TryGetInt32(out var count)
            && count is >= ConceptLimits.MinCount and <= ConceptLimits.MaxCount
            ? count
            : null;

    // Kayıtlı iş başlamadan önceki hazırlık (review r1 M-1): istek iptali hazırlığı kesmez (CancellationToken.None); yine de hata
    // olursa iş "başarısız" biter, böylece şerit kalıcı olarak "sürüyor" kalmaz.
    private static async Task PrepareAsync(ConceptJob job, Func<Task> prepare)
    {
        try
        {
            await prepare().ConfigureAwait(false);
        }
        catch
        {
            job.Finish(Resources.ConceptsText.Unexpected);
            throw;
        }
    }

    private ConceptJob NewJob(ModelCallerIdentity who, ProfileGenerationContext context, JobLane lane, JobKind kind, int total) => new()
    {
        UserId = who.UserId,
        ProfileId = context.ProfileId,
        ProfileName = context.Name,
        Lane = lane,
        Kind = kind,
        Total = total,
        StartedAt = time.GetUtcNow(),
    };

    private async Task<Result<(ModelCallerIdentity Caller, ProfileGenerationContext Context)>> ScopeAsync(Guid? profileId, CancellationToken cancellationToken)
    {
        if (caller.Current() is not { } who)
        {
            return Result<(ModelCallerIdentity, ProfileGenerationContext)>.Failure(ConceptsErrors.SignInRequired);
        }

        return profileId is { } id && await profiles.GetAsync(who.UserId, id, cancellationToken).ConfigureAwait(false) is { } context
            ? Result<(ModelCallerIdentity, ProfileGenerationContext)>.Success((who, context))
            : Result<(ModelCallerIdentity, ProfileGenerationContext)>.Failure(ConceptsErrors.ProfileNotFound);
    }

    private async Task<ReadinessDto> ReadinessAsync(ProfileGenerationContext context, CancellationToken cancellationToken)
    {
        var selectable = (await models.ListAsync(cancellationToken).ConfigureAwait(false))
            .Where(m => m.Selectable)
            .Select(m => m.Id)
            .ToHashSet(StringComparer.Ordinal);
        bool Ok(string? model) => model is not null && selectable.Contains(model);
        var missing = new List<string>();
        foreach (var (role, model) in new[] { ("concept", context.ConceptModel), ("dedupe", context.DedupeModel), ("topic", context.TopicModel) })
        {
            if (!Ok(model))
            {
                missing.Add(role);
            }
        }

        return new ReadinessDto(
            Ok(context.TopicModel) && Ok(context.DedupeModel),
            Ok(context.ConceptModel),
            Ok(context.ConceptModel) && Ok(context.DedupeModel),
            missing);
    }

    private async Task<ConceptWorkDto> WorkAsync(ModelCallerIdentity who, ProfileGenerationContext context, CancellationToken cancellationToken)
    {
        var pending = (await suggestions.ListAsync(who.UserId, context.ProfileId, cancellationToken).ConfigureAwait(false))
            .Where(s => s.State == SuggestionState.Pending)
            .OrderBy(s => s.CreatedAt)
            .Select(s => new SuggestionDto(s.Id, s.Title, s.Summary))
            .ToList();
        ConceptJobDto? JobOf(JobLane lane) =>
            registry.Get(who.UserId, lane) is { } job && job.ProfileId == context.ProfileId ? ConceptJobDto.From(job.Snapshot()) : null;
        return new ConceptWorkDto(
            context.ProfileId,
            await ReadinessAsync(context, cancellationToken).ConfigureAwait(false),
            pending,
            JobOf(JobLane.Topic),
            JobOf(JobLane.Idea));
    }
}
