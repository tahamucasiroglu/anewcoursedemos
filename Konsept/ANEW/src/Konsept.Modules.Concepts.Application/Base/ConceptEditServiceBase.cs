using System.Globalization;
using System.Text.Json;
using Konsept.Modules.AiGateway.Contracts.Abstract;
using Konsept.Modules.AiGateway.Contracts.Dtos;
using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Application.Dtos;
using Konsept.Modules.Concepts.Application.Resources;
using Konsept.Modules.Concepts.Contracts.Abstract;
using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Enums;
using Konsept.Modules.Concepts.Domain.Rules;
using Konsept.Modules.Profiles.Contracts.Abstract;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Concepts.Application.Base;

/// <summary>
/// Konsept düzenleme döngüsü (spec 0015). Sıra: oturum → konsept kullanıcının mı (değilse "bulunamadı", BR-1) → durum düzenlenebilir mi
/// (Üretildi/Onaylandı; S-5) → alan kuralları (sahne sayısı aynı, başlık/seslendirme dolu, süre pozitif tam sayı; talimat dolu) →
/// (yeniden üretimde) konsept modeli seçilebilir mi → aynı konseptte süren yeniden üretim yok mu (G-3). Reddedilen istekte hiçbir online
/// çağrı yapılmaz. Yeniden üretim arka planda koşar, istek hemen döner; sonuç revizyon olarak bellekte bekler, onaylanana ya da silinene
/// kadar konsept değişmez (BR-6). "Video oldu" konsept yalnızca Yeniden üret bağlamında (spec 0020 Req 15, geçerli belirteç) düzenlenir;
/// durumu değişmez.
/// </summary>
/// <param name="caller">Model çağrısı (çağıran kimliği).</param>
/// <param name="profiles">Profil bağlamı (Profiles.Contracts).</param>
/// <param name="models">Seçilebilir metin modelleri.</param>
/// <param name="concepts">Konsept deposu.</param>
/// <param name="revisions">Revizyon kaydı.</param>
/// <param name="launcher">Arka plan başlatıcı.</param>
/// <param name="rules">Kurallar.</param>
/// <param name="regeneration">Yeniden üret bağlamı (spec 0020).</param>
/// <param name="time">Saat.</param>
public abstract class ConceptEditServiceBase(
    IModelCaller caller,
    IProfileGenerationReader profiles,
    ITextModelDirectory models,
    IConceptRepository concepts,
    IConceptRevisionRegistry revisions,
    IConceptJobLauncher launcher,
    IConceptRuleSource rules,
    IConceptRegenerationContext regeneration,
    TimeProvider time) : IConceptEditService
{
    /// <inheritdoc />
    public async Task<Result<ConceptDetailDto>> SaveScenesAsync(Guid conceptId, SaveScenesRequest? request, string? regen, CancellationToken cancellationToken)
    {
        var found = await EditableAsync(conceptId, regen, cancellationToken).ConfigureAwait(false);
        if (!found.IsSuccess)
        {
            return Result<ConceptDetailDto>.Failure(found.Error!);
        }

        var (who, concept) = found.Value;
        var scenes = Validate(request?.Scenes, concept);
        if (!scenes.IsSuccess)
        {
            return Result<ConceptDetailDto>.Failure(scenes.Error!);
        }

        // Elle düzenlemede kelime tekrarı çağrısız yeniden hesaplanır (kutu açıksa); süre uyumu da.
        var context = await profiles.GetAsync(who.UserId, concept.ProfileId, cancellationToken).ConfigureAwait(false);
        var repeats = context?.WordCheck == true && WordRepetition.Exceeds(scenes.Value.Select(s => s.Narration), concept.Language, rules.Rules);
        return await ReplaceAsync(who.UserId, concept, scenes.Value, repeats, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<RevisionDto>> RegenerateAsync(Guid conceptId, RegenerateRequest? request, string? regen, CancellationToken cancellationToken)
    {
        var found = await EditableAsync(conceptId, regen, cancellationToken).ConfigureAwait(false);
        if (!found.IsSuccess)
        {
            return Result<RevisionDto>.Failure(found.Error!);
        }

        var (who, concept) = found.Value;
        var instruction = request?.Instruction?.Trim() ?? string.Empty;
        if (instruction.Length == 0)
        {
            return Result<RevisionDto>.Failure(ConceptsErrors.InstructionRequired);
        }

        if (instruction.Length > ConceptLimits.MaxInstructionLength)
        {
            return Result<RevisionDto>.Failure(ConceptsErrors.InstructionTooLong);
        }

        IReadOnlyList<ConceptScene>? unsaved = null;
        if (request!.Scenes is not null)
        {
            var scenes = Validate(request.Scenes, concept);
            if (!scenes.IsSuccess)
            {
                return Result<RevisionDto>.Failure(scenes.Error!);
            }

            unsaved = scenes.Value;
        }

        if (await profiles.GetAsync(who.UserId, concept.ProfileId, cancellationToken).ConfigureAwait(false) is not { } context)
        {
            return Result<RevisionDto>.Failure(ConceptsErrors.NotFound);
        }

        var ready = (await models.ListAsync(cancellationToken).ConfigureAwait(false))
            .Any(m => m.Selectable && string.Equals(m.Id, context.ConceptModel, StringComparison.Ordinal));
        if (!ready)
        {
            return Result<RevisionDto>.Failure(ConceptsErrors.NotReady);
        }

        // Taban: kaydedilmemiş elle değişiklikler (G-2) → hazır revizyon (Tekrar düzenle revizyon üzerinde ilerler) → kayıtlı konsept.
        var previous = revisions.Get(who.UserId, concept.Id)?.Snapshot();
        var baseScenes = unsaved ?? (previous is { Status: RevisionStatus.Ready } p ? p.Scenes : concept.Scenes);
        var revision = new ConceptRevision
        {
            UserId = who.UserId,
            ConceptId = concept.Id,
            Fallback = previous is { Status: RevisionStatus.Ready } kept ? kept : null, // Başarısız tur hazır revizyonu silmez (M-1).
        };
        if (!revisions.TryStart(revision))
        {
            return Result<RevisionDto>.Failure(ConceptsErrors.RevisionBusy); // Çift tıklama / ikinci istek: çağrı yok (G-3).
        }

        launcher.LaunchRevision(revision, new RevisionInput(who, context, concept, baseScenes, instruction));
        return Result<RevisionDto>.Success(RevisionDto.From(revision.Snapshot()));
    }

    /// <inheritdoc />
    public async Task<Result<RevisionDto>> GetRevisionAsync(Guid conceptId, CancellationToken cancellationToken)
    {
        var found = await OwnedAsync(conceptId, cancellationToken).ConfigureAwait(false);
        if (!found.IsSuccess)
        {
            return Result<RevisionDto>.Failure(found.Error!);
        }

        var revision = revisions.Get(found.Value.Caller.UserId, conceptId);
        return Result<RevisionDto>.Success(revision is null ? RevisionDto.None(conceptId) : RevisionDto.From(revision.Snapshot()));
    }

    /// <inheritdoc />
    public async Task<Result<ConceptDetailDto>> ApproveRevisionAsync(Guid conceptId, string? regen, CancellationToken cancellationToken)
    {
        var found = await EditableAsync(conceptId, regen, cancellationToken).ConfigureAwait(false);
        if (!found.IsSuccess)
        {
            return Result<ConceptDetailDto>.Failure(found.Error!);
        }

        var (who, concept) = found.Value;
        if (revisions.Get(who.UserId, conceptId)?.Snapshot() is not { Status: RevisionStatus.Ready } ready)
        {
            return Result<ConceptDetailDto>.Failure(ConceptsErrors.RevisionNotReady);
        }

        var result = await ReplaceAsync(who.UserId, concept, ready.Scenes, ready.WordRepetitionWarning, cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            revisions.Remove(who.UserId, conceptId, ready.Id);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<RevisionDto>> DiscardRevisionAsync(Guid conceptId, CancellationToken cancellationToken)
    {
        var found = await OwnedAsync(conceptId, cancellationToken).ConfigureAwait(false);
        if (!found.IsSuccess)
        {
            return Result<RevisionDto>.Failure(found.Error!);
        }

        var userId = found.Value.Caller.UserId;
        if (revisions.Get(userId, conceptId) is { } revision)
        {
            if (revision.IsRunning)
            {
                return Result<RevisionDto>.Failure(ConceptsErrors.RevisionBusy);
            }

            revisions.Remove(userId, conceptId, revision.Id);
        }

        return Result<RevisionDto>.Success(RevisionDto.None(conceptId));
    }

    /// <summary>Süre: yalnızca JSON tam sayısı (ondalık, metin, boş → <c>null</c>).</summary>
    /// <param name="value">Ham değer.</param>
    /// <returns>Süre ya da <c>null</c>.</returns>
    protected static int? DurationOf(JsonElement? value) =>
        value is { ValueKind: JsonValueKind.Number } n && n.TryGetInt32(out var seconds) ? seconds : null;

    private static Result<IReadOnlyList<ConceptScene>> Validate(IReadOnlyList<SceneInput>? input, Concept concept)
    {
        if (input is null || input.Count == 0 || input.Count != concept.Scenes.Count)
        {
            return Result<IReadOnlyList<ConceptScene>>.Failure(ConceptsErrors.SceneCountInvalid); // Ekleme/silme yok (G-20).
        }

        var drafts = input.Select(s => new SceneDraft(s?.Title, s?.Visual, s?.Narration, DurationOf(s?.DurationSeconds))).ToList();
        var issues = SceneEdit.Issues(drafts);
        if (issues.Count == 0)
        {
            return Result<IReadOnlyList<ConceptScene>>.Success(SceneEdit.ToScenes(drafts));
        }

        var errors = issues.ToDictionary(
            i => "Scenes[" + i.Index.ToString(CultureInfo.InvariantCulture) + "]." + i.Field,
            i => i.Field switch
            {
                SceneEdit.TitleField => ConceptsText.SceneTitleInvalid,
                SceneEdit.VisualField => ConceptsText.SceneVisualInvalid,
                SceneEdit.NarrationField => ConceptsText.SceneNarrationInvalid,
                _ => ConceptsText.SceneDurationInvalid,
            },
            StringComparer.Ordinal);
        return Result<IReadOnlyList<ConceptScene>>.Failure(ConceptsErrors.SceneFields(errors));
    }

    private async Task<Result<ConceptDetailDto>> ReplaceAsync(
        Guid userId, Concept concept, IReadOnlyList<ConceptScene> scenes, bool wordRepetition, CancellationToken cancellationToken)
    {
        var mismatch = DurationFit.IsMismatch(scenes.Sum(s => s.DurationSeconds), concept.TargetDurationSeconds, rules.Rules.DurationTolerance);
        // Video oldu konsept yalnızca bağlamda buraya gelir (EditableAsync); durum koşulu yazımın içinde kalır.
        IReadOnlyList<ConceptStatus> allowed = concept.Status == ConceptStatus.VideoReady ? [ConceptStatus.VideoReady] : ConceptCodes.Editable;
        var changed = await concepts.UpdateScenesAsync(
            userId, concept.Id, allowed, scenes, wordRepetition, mismatch, time.GetUtcNow(), CancellationToken.None).ConfigureAwait(false);
        if (!changed)
        {
            return Result<ConceptDetailDto>.Failure(ConceptsErrors.LockedQueued); // Bu arada durum değişti.
        }

        var updated = await concepts.GetManyAsync(userId, [concept.Id], cancellationToken).ConfigureAwait(false);
        return updated.Count == 0
            ? Result<ConceptDetailDto>.Failure(ConceptsErrors.NotFound)
            : Result<ConceptDetailDto>.Success(ConceptDetailDto.From(updated[0]));
    }

    private async Task<Result<(ModelCallerIdentity Caller, Concept Concept)>> OwnedAsync(Guid conceptId, CancellationToken cancellationToken)
    {
        if (caller.Current() is not { } who)
        {
            return Result<(ModelCallerIdentity, Concept)>.Failure(ConceptsErrors.SignInRequired);
        }

        var found = await concepts.GetManyAsync(who.UserId, [conceptId], cancellationToken).ConfigureAwait(false);
        return found.Count == 0
            ? Result<(ModelCallerIdentity, Concept)>.Failure(ConceptsErrors.NotFound)
            : Result<(ModelCallerIdentity, Concept)>.Success((who, found[0]));
    }

    private async Task<Result<(ModelCallerIdentity Caller, Concept Concept)>> EditableAsync(Guid conceptId, string? regen, CancellationToken cancellationToken)
    {
        var found = await OwnedAsync(conceptId, cancellationToken).ConfigureAwait(false);
        if (!found.IsSuccess)
        {
            return found;
        }

        return found.Value.Concept.Status switch
        {
            ConceptStatus.Generated or ConceptStatus.Approved => found,
            ConceptStatus.VideoReady when regeneration.IsOpen(found.Value.Caller.UserId, conceptId, regen) => found, // Yeniden üret bağlamı (spec 0020 Req 15).
            ConceptStatus.VideoReady => Result<(ModelCallerIdentity, Concept)>.Failure(ConceptsErrors.LockedVideo),
            _ => Result<(ModelCallerIdentity, Concept)>.Failure(ConceptsErrors.LockedQueued),
        };
    }
}
