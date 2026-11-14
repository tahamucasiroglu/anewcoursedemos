using TahaKonsept.Modules.Identity.Domain.Enums;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Mapping;

/// <summary>
/// Sözleşme enum'ları (Shared.Contracts) ile domain enum'ları arasında açık eşleme: tema ve istemci türü.
/// </summary>
public static class IdentityEnumMapper
{
    /// <summary>Sözleşme istemci türünü domain türüne çevirir.</summary>
    /// <param name="kind">Sözleşme türü.</param>
    /// <returns>Domain türü.</returns>
    public static RefreshTokenKind ToDomain(TokenClientKind kind) => kind switch
    {
        TokenClientKind.Agent => RefreshTokenKind.Agent,
        _ => RefreshTokenKind.Web,
    };

    /// <summary>Sözleşme temasını domain temasına çevirir.</summary>
    /// <param name="theme">Sözleşme teması.</param>
    /// <returns>Domain teması.</returns>
    public static UserTheme ToDomain(ThemePreference theme) => theme switch
    {
        ThemePreference.Light => UserTheme.Light,
        ThemePreference.Dark => UserTheme.Dark,
        _ => UserTheme.System,
    };

    /// <summary>Domain temasını sözleşme temasına çevirir.</summary>
    /// <param name="theme">Domain teması.</param>
    /// <returns>Sözleşme teması.</returns>
    public static ThemePreference ToContract(UserTheme theme) => theme switch
    {
        UserTheme.Light => ThemePreference.Light,
        UserTheme.Dark => ThemePreference.Dark,
        _ => ThemePreference.System,
    };
}
