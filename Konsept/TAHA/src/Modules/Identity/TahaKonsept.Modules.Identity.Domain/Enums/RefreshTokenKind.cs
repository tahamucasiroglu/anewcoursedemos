namespace TahaKonsept.Modules.Identity.Domain.Enums;

/// <summary>
/// Refresh token'ın ait olduğu istemci türü (DB'de metin olarak saklanır).
/// </summary>
public enum RefreshTokenKind
{
    /// <summary>Web (MVC BFF) oturumu.</summary>
    Web = 0,

    /// <summary>Masaüstü ajanı.</summary>
    Agent = 1,
}
