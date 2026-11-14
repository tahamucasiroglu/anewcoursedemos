using TahaKonsept.Modules.Profiles.Domain.Entities;
using TahaKonsept.Modules.Profiles.Domain.ValueObjects;
using TahaKonsept.Shared.Contracts.Profiles;

namespace TahaKonsept.Modules.Profiles.Application.Mapping;

/// <summary>
/// Profil eşlemeleri (elle, AutoMapper yok): <see cref="ToDto"/> (entity → DTO), <see cref="ToDetails"/> (istek → değer nesnesi).
/// </summary>
public static class ProfileMappings
{
    /// <summary>Entity'yi DTO'ya çevirir.</summary>
    /// <param name="profile">Profil.</param>
    /// <returns>DTO.</returns>
    public static ProfileDto ToDto(this Profile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return new ProfileDto
        {
            Id = profile.Id,
            Name = profile.Name,
            Language = profile.Language,
            Format = profile.Format,
            TargetDurationSec = profile.TargetDurationSec,
            ChannelDescription = profile.ChannelDescription,
            DefaultTone = profile.DefaultTone,
            DefaultAudience = profile.DefaultAudience,
            DefaultNarrativeStyle = profile.DefaultNarrativeStyle,
            TemplateKey = profile.TemplateKey,
            IsActive = profile.IsActive,
            CreatedAt = profile.CreatedAtUtc,
            UpdatedAt = profile.UpdatedAtUtc,
        };
    }

    /// <summary>İsteği değer nesnesine çevirir.</summary>
    /// <param name="request">İstek.</param>
    /// <returns>Ayrıntılar.</returns>
    public static ProfileDetails ToDetails(this SaveProfileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new ProfileDetails(
            request.Name,
            request.Language,
            request.Format,
            request.TargetDurationSec,
            request.ChannelDescription ?? string.Empty,
            request.DefaultTone,
            request.DefaultAudience,
            request.DefaultNarrativeStyle,
            request.TemplateKey);
    }
}
