using Konsept.Modules.AiGateway.Domain.Enums;

namespace Konsept.Modules.AiGateway.Domain.Entities;

/// <summary>
/// Kullanıcının bir sağlayıcı için kayıtlı API key'i (spec 0004 Req 7–9, H-2): yalnızca şifreli değer, maskeli ipucu, doğrulama
/// etiketi ve güncellenme zamanı. Düz key hiçbir alanda tutulmaz. Kullanıcı + sağlayıcı başına en fazla bir satır.
/// </summary>
public sealed class UserApiKey
{
    /// <summary>Gets satır kimliği.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets sahibi (BR-1).</summary>
    public Guid UserId { get; init; }

    /// <summary>Gets sağlayıcı kimliği.</summary>
    public required string ProviderId { get; init; }

    /// <summary>Gets Data Protection ile şifrelenmiş key.</summary>
    public required string Ciphertext { get; init; }

    /// <summary>Gets maskeli ipucu (<see cref="Rules.ApiKeyMask"/>).</summary>
    public required string Hint { get; init; }

    /// <summary>Gets doğrulama etiketi.</summary>
    public KeyValidation Validation { get; init; }

    /// <summary>Gets son güncellenme zamanı (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; init; }
}
