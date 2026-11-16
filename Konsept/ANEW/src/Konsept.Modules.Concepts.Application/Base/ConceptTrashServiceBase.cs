using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Application.Dtos;
using Konsept.Modules.Concepts.Domain.Enums;
using Konsept.Modules.Concepts.Domain.Rules;
using Konsept.Modules.Profiles.Contracts.Abstract;
using Konsept.Shared.Kernel.Abstract;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Concepts.Application.Base;

/// <summary>
/// Silinenler ve önerileri sıfırla (spec 0016). Silme soft delete'tir ve yalnızca Üretildi/Onaylandı'da (Sırada/Üretiliyor/Video oldu
/// reddedilir, konsept değişmez; S-5); Silinenler'deki kayıt süresiz durur (BR-7) ve öneri geçmişinde "elenen" sayılır (BR-8, depo).
/// Kalıcı silme yalnızca Silinenler'deki kaydı kaldırır; konsept konudan geldiyse aynı profilde aynı anahtarlı "seçildi" öneri hafızası
/// da gider — başlık negatif sinyal olmaktan çıkar (G-9). Sıfırlama yalnızca seçili profilin önerilip seçilmemiş başlıklarını siler
/// (G-10). Başkasının kaydı her yerde "bulunamadı" (BR-1); online çağrı yoktur.
/// </summary>
/// <param name="concepts">Konsept deposu.</param>
/// <param name="suggestions">Öneri deposu.</param>
/// <param name="profiles">Profil bağlamı (sahiplik).</param>
/// <param name="currentUser">İsteği yapan kullanıcı.</param>
/// <param name="time">Saat.</param>
public abstract class ConceptTrashServiceBase(
    IConceptRepository concepts,
    ITopicSuggestionRepository suggestions,
    IProfileGenerationReader profiles,
    ICurrentUser currentUser,
    TimeProvider time) : IConceptTrashService
{
    /// <inheritdoc />
    public async Task<Result<DeletedConceptItemDto>> DeleteAsync(Guid conceptId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<DeletedConceptItemDto>.Failure(ConceptsErrors.SignInRequired);
        }

        var found = await concepts.GetManyAsync(userId, [conceptId], cancellationToken).ConfigureAwait(false);
        if (found.Count == 0)
        {
            return Result<DeletedConceptItemDto>.Failure(ConceptsErrors.NotFound);
        }

        var concept = found[0];
        if (concept.Status == ConceptStatus.VideoReady)
        {
            return Result<DeletedConceptItemDto>.Failure(ConceptsErrors.DeleteLockedVideo);
        }

        if (!ConceptCodes.Editable.Contains(concept.Status))
        {
            return Result<DeletedConceptItemDto>.Failure(ConceptsErrors.LockedQueued);
        }

        var at = time.GetUtcNow();
        if (!await concepts.SoftDeleteAsync(userId, conceptId, ConceptCodes.Editable, at, CancellationToken.None).ConfigureAwait(false))
        {
            return Result<DeletedConceptItemDto>.Failure(ConceptsErrors.LockedQueued); // Bu arada durum değişti.
        }

        return Result<DeletedConceptItemDto>.Success(new DeletedConceptItemDto(concept.Id, concept.Title, at));
    }

    /// <inheritdoc />
    public async Task<Result<DeletedConceptListDto>> ListAsync(Guid profileId, int page, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<DeletedConceptListDto>.Failure(ConceptsErrors.SignInRequired);
        }

        if (await profiles.GetAsync(userId, profileId, cancellationToken).ConfigureAwait(false) is null)
        {
            return Result<DeletedConceptListDto>.Failure(ConceptsErrors.ProfileNotFound);
        }

        var number = Math.Max(1, page);
        var result = await concepts.ListDeletedAsync(userId, profileId, (number - 1) * ConceptLimits.PageSize, ConceptLimits.PageSize, cancellationToken)
            .ConfigureAwait(false);
        return Result<DeletedConceptListDto>.Success(new DeletedConceptListDto(
            [.. result.Items.Select(c => new DeletedConceptItemDto(c.Id, c.Title, c.DeletedAt ?? c.UpdatedAt))], number, ConceptLimits.PageSize, result.Total));
    }

    /// <inheritdoc />
    public async Task<Result<ConceptDetailDto>> GetAsync(Guid conceptId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<ConceptDetailDto>.Failure(ConceptsErrors.SignInRequired);
        }

        return await concepts.GetDeletedAsync(userId, conceptId, cancellationToken).ConfigureAwait(false) is { } concept
            ? Result<ConceptDetailDto>.Success(ConceptDetailDto.From(concept))
            : Result<ConceptDetailDto>.Failure(ConceptsErrors.NotFound);
    }

    /// <inheritdoc />
    public async Task<Result<DeletedConceptItemDto>> PurgeAsync(Guid conceptId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<DeletedConceptItemDto>.Failure(ConceptsErrors.SignInRequired);
        }

        if (await concepts.GetDeletedAsync(userId, conceptId, cancellationToken).ConfigureAwait(false) is not { } concept
            || !await concepts.PurgeAsync(userId, conceptId, CancellationToken.None).ConfigureAwait(false))
        {
            return Result<DeletedConceptItemDto>.Failure(ConceptsErrors.NotFound);
        }

        if (concept.Source == ConceptSource.Topic)
        {
            await suggestions.DeleteSelectedAsync(userId, concept.ProfileId, concept.TitleKey, CancellationToken.None).ConfigureAwait(false);
        }

        return Result<DeletedConceptItemDto>.Success(new DeletedConceptItemDto(concept.Id, concept.Title, concept.DeletedAt ?? concept.UpdatedAt));
    }

    /// <inheritdoc />
    public async Task<Result<ResetSuggestionsDto>> ResetSuggestionsAsync(ResetSuggestionsRequest? request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<ResetSuggestionsDto>.Failure(ConceptsErrors.SignInRequired);
        }

        if (request?.ProfileId is not { } profileId
            || await profiles.GetAsync(userId, profileId, cancellationToken).ConfigureAwait(false) is null)
        {
            return Result<ResetSuggestionsDto>.Failure(ConceptsErrors.ProfileNotFound);
        }

        var cleared = await suggestions.DeleteRejectedAsync(userId, profileId, CancellationToken.None).ConfigureAwait(false);
        return Result<ResetSuggestionsDto>.Success(new ResetSuggestionsDto(profileId, cleared));
    }
}
