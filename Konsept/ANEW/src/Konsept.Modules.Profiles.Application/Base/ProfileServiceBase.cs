using Konsept.Modules.Concepts.Contracts.Abstract;
using Konsept.Modules.Profiles.Application.Abstract;
using Konsept.Modules.Profiles.Application.Dtos;
using Konsept.Modules.Profiles.Domain.Entities;
using Konsept.Modules.Profiles.Domain.Rules;
using Konsept.Shared.Kernel.Abstract;
using Konsept.Shared.Kernel.Results;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Profiles.Application.Base;

/// <summary>
/// Profil yönetimi (spec 0003). Her işlem isteği yapan kullanıcının kimliğiyle yapılır (BR-1, H-1): başka kullanıcının profil
/// kimliği "Profil bulunamadı" döner, varlığı açığa çıkmaz. Alan kuralları <see cref="ProfileRules"/>'tadır ve arayüzü atlayan
/// isteklerde de uygulanır (Req 6). Seçili profil okunurken çözülür: hatırlanan profil kullanıcının değilse ya da silinmişse ilk
/// oluşturulan profil seçilidir (Req 9). Konsepti olan profil silinemez; kural sunucudadır ve <c>CanDelete</c> ile arayüze yansır
/// (Req 14, spec 0014 Req 26). Loglarda kanal içeriği yoktur.
/// </summary>
/// <param name="profiles">Profil deposu.</param>
/// <param name="templates">Şablonlar.</param>
/// <param name="concepts">Konsepti olan profiller (Concepts.Contracts).</param>
/// <param name="currentUser">İsteği yapan kullanıcı.</param>
/// <param name="time">Saat.</param>
/// <param name="logger">Log.</param>
public abstract class ProfileServiceBase(
    IProfileRepository profiles,
    IProfileTemplateSource templates,
    IProfileConceptPresence concepts,
    ICurrentUser currentUser,
    TimeProvider time,
    ILogger logger) : IProfileService
{
    private static readonly Action<ILogger, Guid, Exception?> Created = LoggerMessage.Define<Guid>(
        LogLevel.Information, new EventId(3001, "ProfileCreated"), "Profil oluşturuldu: {ProfileId}");

    private static readonly Action<ILogger, Guid, Exception?> Updated = LoggerMessage.Define<Guid>(
        LogLevel.Information, new EventId(3002, "ProfileUpdated"), "Profil güncellendi: {ProfileId}");

    private static readonly Action<ILogger, Guid, Exception?> Deleted = LoggerMessage.Define<Guid>(
        LogLevel.Information, new EventId(3003, "ProfileDeleted"), "Profil silindi: {ProfileId}");

    /// <inheritdoc />
    public async Task<Result<ProfileListDto>> ListAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<ProfileListDto>.Failure(ProfilesErrors.SignInRequired);
        }

        return Result<ProfileListDto>.Success(await BuildListAsync(userId, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<Result<ProfileDto>> CreateAsync(SaveProfileRequest? request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<ProfileDto>.Failure(ProfilesErrors.SignInRequired);
        }

        if (Invalid(request) is { } invalid)
        {
            return Result<ProfileDto>.Failure(invalid);
        }

        var existing = await profiles.ListAsync(userId, cancellationToken).ConfigureAwait(false);
        if (existing.Count >= ProfileRules.MaxProfiles)
        {
            return Result<ProfileDto>.Failure(ProfilesErrors.LimitReached);
        }

        var now = time.GetUtcNow();
        var profile = Map(Guid.CreateVersion7(), userId, request!, now, now);
        if (existing.Any(p => p.NameKey == profile.NameKey) || !await profiles.AddAsync(profile, cancellationToken).ConfigureAwait(false))
        {
            return Result<ProfileDto>.Failure(ProfilesErrors.NameTaken);
        }

        // Yeni profil kendiliğinden seçili profil olur (Req 3).
        await profiles.SetSelectionAsync(userId, profile.Id, cancellationToken).ConfigureAwait(false);
        Created(logger, profile.Id, null);
        return Result<ProfileDto>.Success(ToDto(profile, withConcepts: false));
    }

    /// <inheritdoc />
    public async Task<Result<ProfileDto>> UpdateAsync(Guid profileId, SaveProfileRequest? request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<ProfileDto>.Failure(ProfilesErrors.SignInRequired);
        }

        if (await profiles.GetAsync(userId, profileId, cancellationToken).ConfigureAwait(false) is not { } current)
        {
            return Result<ProfileDto>.Failure(ProfilesErrors.NotFound);
        }

        if (Invalid(request) is { } invalid)
        {
            return Result<ProfileDto>.Failure(invalid);
        }

        var profile = Map(current.Id, userId, request!, current.CreatedAt, time.GetUtcNow());
        var existing = await profiles.ListAsync(userId, cancellationToken).ConfigureAwait(false);
        if (existing.Any(p => p.Id != profile.Id && p.NameKey == profile.NameKey) || !await profiles.UpdateAsync(profile, cancellationToken).ConfigureAwait(false))
        {
            return Result<ProfileDto>.Failure(ProfilesErrors.NameTaken);
        }

        Updated(logger, profile.Id, null);
        return Result<ProfileDto>.Success(await ToDtoAsync(profile, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<Result<ProfileListDto>> DeleteAsync(Guid profileId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<ProfileListDto>.Failure(ProfilesErrors.SignInRequired);
        }

        if (await profiles.GetAsync(userId, profileId, cancellationToken).ConfigureAwait(false) is null)
        {
            return Result<ProfileListDto>.Failure(ProfilesErrors.NotFound);
        }

        // Konsepti olan profil silinemez; arayüzü atlayan istek de reddedilir, profil yerinde kalır (spec 0014 Req 26, AC-30).
        var withConcepts = await concepts.ProfilesWithConceptsAsync(userId, cancellationToken).ConfigureAwait(false);
        if (withConcepts.Contains(profileId))
        {
            return Result<ProfileListDto>.Failure(ProfilesErrors.HasConcepts);
        }

        if (!await profiles.DeleteAsync(userId, profileId, cancellationToken).ConfigureAwait(false))
        {
            return Result<ProfileListDto>.Failure(ProfilesErrors.NotFound);
        }

        Deleted(logger, profileId, null);
        return Result<ProfileListDto>.Success(await BuildListAsync(userId, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<Result<ProfileDto>> SelectAsync(SelectProfileRequest? request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result<ProfileDto>.Failure(ProfilesErrors.SignInRequired);
        }

        if (request?.ProfileId is not { } profileId
            || await profiles.GetAsync(userId, profileId, cancellationToken).ConfigureAwait(false) is not { } profile)
        {
            return Result<ProfileDto>.Failure(ProfilesErrors.NotFound);
        }

        await profiles.SetSelectionAsync(userId, profile.Id, cancellationToken).ConfigureAwait(false);
        return Result<ProfileDto>.Success(await ToDtoAsync(profile, cancellationToken).ConfigureAwait(false));
    }

    private static Error? Invalid(SaveProfileRequest? request)
    {
        var issues = ProfileRules.Check(request?.ChannelName, request?.Language, request?.Format, request?.DurationSeconds, request?.Content);
        return issues.Count == 0 ? null : ProfilesErrors.FromIssues(issues);
    }

    private static Profile Map(Guid id, Guid userId, SaveProfileRequest request, DateTimeOffset createdAt, DateTimeOffset updatedAt) => new()
    {
        Id = id,
        UserId = userId,
        Name = ProfileRules.Clean(request.ChannelName),
        NameKey = ProfileRules.NameKey(request.ChannelName),
        Language = request.Language!,
        Format = request.Format!,
        DurationSeconds = request.DurationSeconds!.Value,
        Content = ProfileRules.Clean(request.Content),
        CreatedAt = createdAt,
        UpdatedAt = updatedAt,
    };

    // Konsepti olan profil silinemez (Req 14, spec 0014 Req 26).
    private static ProfileDto ToDto(Profile p, bool withConcepts) =>
        new(p.Id, p.Name, p.Language, p.Format, p.DurationSeconds, p.Content, p.CreatedAt, CanDelete: !withConcepts);

    private async Task<ProfileDto> ToDtoAsync(Profile profile, CancellationToken cancellationToken)
    {
        var withConcepts = await concepts.ProfilesWithConceptsAsync(profile.UserId, cancellationToken).ConfigureAwait(false);
        return ToDto(profile, withConcepts.Contains(profile.Id));
    }

    private async Task<ProfileListDto> BuildListAsync(Guid userId, CancellationToken cancellationToken)
    {
        var items = await profiles.ListAsync(userId, cancellationToken).ConfigureAwait(false);
        var remembered = await profiles.GetSelectionAsync(userId, cancellationToken).ConfigureAwait(false);
        var selected = items.FirstOrDefault(p => p.Id == remembered)
            ?? items.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id).FirstOrDefault();
        var withConcepts = await concepts.ProfilesWithConceptsAsync(userId, cancellationToken).ConfigureAwait(false);
        var ordered = items.OrderBy(p => p.Name, ProfileRules.NameOrder).ThenBy(p => p.CreatedAt)
            .Select(p => ToDto(p, withConcepts.Contains(p.Id)))
            .ToList();
        var templateList = templates.Templates
            .Select(t => new ProfileTemplateDto(t.Id, t.Title, t.ChannelName, t.Language, t.Format, t.DurationSeconds, t.Content))
            .ToList();
        return new ProfileListDto(ordered, selected?.Id, ProfileRules.MaxProfiles, templateList);
    }
}
