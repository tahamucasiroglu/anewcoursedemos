namespace Konsept.Modules.AiGateway.Domain.Enums;

/// <summary>API key biçim sorunu (<see cref="Rules.ApiKeyFormat.Check"/>).</summary>
public enum ApiKeyFormatIssue
{
    /// <summary>Sorun yok.</summary>
    None,

    /// <summary>Boş ya da yalnızca boşluk.</summary>
    Empty,

    /// <summary>8 karakterden kısa.</summary>
    TooShort,

    /// <summary>512 karakterden uzun.</summary>
    TooLong,

    /// <summary>Kontrol karakteri içeriyor.</summary>
    ControlCharacter,
}
