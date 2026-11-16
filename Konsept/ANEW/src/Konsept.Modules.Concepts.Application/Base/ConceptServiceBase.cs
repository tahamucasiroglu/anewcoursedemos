using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Application.Dtos;
using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Enums;
using Konsept.Modules.Concepts.Domain.Rules;
using Konsept.Modules.Profiles.Contracts.Abstract;
using Konsept.Shared.Kernel.Abstract;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Concepts.Application.Base;

/// <summary>
/// Üretilenler ve onay (spec 0014 Req 22–25, BR-16, S-1). Liste seçili profilin silinmemiş konseptleridir; varsayılan filtre
/// Üretildi + Onaylandı, en yeni üstte, 50'şerli. Onay yalnızca "Üretildi"den "Onaylandı"ya, geri alma yalnızca "Onaylandı"dan
/// "Üretildi"ye; istekteki konseptlerden biri bile başkasınınsa "bulunamadı", uygun durumda değilse red — ikisinde de hiçbiri
/// değişmez (BR-1).
/// </summary>
/// <param name="concepts">Konsept deposu.</param>
/// <param name="profiles">Profil bağlamı (sahiplik).</param>
/// <param name="currentUser">İsteği yapan kullanıcı.</param>
/// <param name="time">Saat.</param>
public abstract class ConceptServiceBase(
    IConceptRepository concepts,
    IProfileGenerationReader profiles,
    ICurrentUser currentUser,
    TimeProvider time) : IConceptService
{
    /// <inheritdoc />
    public async Task<Result<ConceptListDto>> ListAsync(Guid profileId, string? statuses, int page, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<ConceptListDto>.Failure(ConceptsErrors.SignInRequired);
        }

        if (await profiles.GetAsync(userId, profileId, cancellationToken).ConfigureAwait(false) is null)
        {
            return Result<ConceptListDto>.Failure(ConceptsErrors.ProfileNotFound);
        }

        var filter = new List<ConceptStatus>();
        foreach (var code in (statuses ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!ConceptCodes.TryParse(code, out var status))
            {
                return Result<ConceptListDto>.Failure(ConceptsErrors.StatusInvalid);
            }

            filter.Add(status);
        }

        if (filter.Count == 0)
        {
            filter.AddRange(ConceptCodes.DefaultFilter);
        }

        var number = Math.Max(1, page);
        var result = await concepts.ListAsync(userId, profileId, filter, (number - 1) * ConceptLimits.PageSize, ConceptLimits.PageSize, cancellationToken)
            .ConfigureAwait(false);
        return Result<ConceptListDto>.Success(new ConceptListDto(
            [.. result.Items.Select(Item)], number, ConceptLimits.PageSize, result.Total, [.. filter.Distinct().Select(ConceptCodes.Of)]));
    }

    /// <inheritdoc />
    public async Task<Result<ConceptDetailDto>> GetAsync(Guid conceptId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<ConceptDetailDto>.Failure(ConceptsErrors.SignInRequired);
        }

        var found = await concepts.GetManyAsync(userId, [conceptId], cancellationToken).ConfigureAwait(false);
        if (found.Count == 0)
        {
            return Result<ConceptDetailDto>.Failure(ConceptsErrors.NotFound);
        }

        var c = found[0];
        return Result<ConceptDetailDto>.Success(new ConceptDetailDto(
            c.Id,
            c.ProfileId,
            c.Title,
            ConceptCodes.Of(c.Status),
            c.Language,
            c.Format,
            c.TargetDurationSeconds,
            c.Scenes,
            c.WordRepetitionWarning,
            c.DurationMismatchWarning,
            c.CreatedAt));
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<ConceptListItemDto>>> ApproveAsync(ConceptIdsRequest? request, CancellationToken cancellationToken) =>
        ChangeAsync(request, ConceptStatus.Generated, ConceptStatus.Approved, ConceptsErrors.ApproveInvalidState, cancellationToken);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<ConceptListItemDto>>> UnapproveAsync(ConceptIdsRequest? request, CancellationToken cancellationToken) =>
        ChangeAsync(request, ConceptStatus.Approved, ConceptStatus.Generated, ConceptsErrors.UnapproveInvalidState, cancellationToken);

    private static ConceptListItemDto Item(Concept c) =>
        new(c.Id, c.Title, ConceptCodes.Of(c.Status), c.WordRepetitionWarning, c.DurationMismatchWarning, c.CreatedAt);

    private async Task<Result<IReadOnlyList<ConceptListItemDto>>> ChangeAsync(
        ConceptIdsRequest? request, ConceptStatus from, ConceptStatus to, Error invalidState, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<IReadOnlyList<ConceptListItemDto>>.Failure(ConceptsErrors.SignInRequired);
        }

        var ids = request?.Ids?.Distinct().ToList() ?? [];
        if (ids.Count == 0)
        {
            return Result<IReadOnlyList<ConceptListItemDto>>.Failure(ConceptsErrors.NotFound);
        }

        var found = await concepts.GetManyAsync(userId, ids, cancellationToken).ConfigureAwait(false);
        if (found.Count != ids.Count)
        {
            return Result<IReadOnlyList<ConceptListItemDto>>.Failure(ConceptsErrors.NotFound);
        }

        if (found.Any(c => c.Status != from))
        {
            return Result<IReadOnlyList<ConceptListItemDto>>.Failure(invalidState);
        }

        await concepts.ChangeStatusAsync(userId, ids, from, to, time.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        var updated = await concepts.GetManyAsync(userId, ids, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<ConceptListItemDto>>.Success([.. updated.Select(Item)]);
    }
}
