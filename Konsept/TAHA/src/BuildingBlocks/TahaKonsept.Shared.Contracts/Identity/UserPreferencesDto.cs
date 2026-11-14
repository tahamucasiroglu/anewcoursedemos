namespace TahaKonsept.Shared.Contracts.Identity;

/// <summary>
/// Kullanıcı tercihleri (<c>GET|PUT api/identity/me/preferences</c>). Şimdilik yalnızca tema; aylık bütçe Providers modülündedir.
/// </summary>
public sealed record UserPreferencesDto
{
    /// <summary>Tema tercihi.</summary>
    public ThemePreference Theme { get; init; } = ThemePreference.System;
}
