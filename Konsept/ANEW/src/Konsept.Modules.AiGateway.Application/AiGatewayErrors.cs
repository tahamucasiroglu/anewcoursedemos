using Konsept.Modules.AiGateway.Application.Resources;
using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.AiGateway.Application;

/// <summary>AiGateway'in beklenen hataları (kararlı kod + Türkçe mesaj). Hiçbiri girilen key'i içermez.</summary>
public static class AiGatewayErrors
{
    /// <summary>Hata kodu: sağlayıcı key'i reddetti (401).</summary>
    public const string KeyRejectedCode = "ai.key_rejected";

    /// <summary>Hata kodu: key biçimi geçersiz.</summary>
    public const string KeyInvalidCode = "ai.key_invalid";

    /// <summary>Hata kodu: bilinmeyen sağlayıcı.</summary>
    public const string UnknownProviderCode = "ai.unknown_provider";

    /// <summary>Alan hatası anahtarı (istekteki özellik adı).</summary>
    public const string KeyField = "ApiKey";

    /// <summary>Gets sağlayıcı key'i reddetti; alanın yanında gösterilir.</summary>
    public static Error KeyRejected => FieldError(KeyRejectedCode, AiGatewayText.ApiKeyRejected);

    /// <summary>Gets bilinmeyen sağlayıcı.</summary>
    public static Error UnknownProvider => new(UnknownProviderCode, AiGatewayText.UnknownProvider, ErrorType.NotFound);

    /// <summary>Gets oturum yok.</summary>
    public static Error SignInRequired => new("auth.required", AiGatewayText.SignInRequired, ErrorType.Unauthorized);

    /// <summary>Biçim hatası (alanın yanında).</summary>
    /// <param name="message">Türkçe mesaj.</param>
    /// <returns>Hata.</returns>
    public static Error KeyInvalid(string message) => FieldError(KeyInvalidCode, message);

    private static Error FieldError(string code, string message) =>
        new(code, message, ErrorType.Validation)
        {
            FieldErrors = new Dictionary<string, string[]>(StringComparer.Ordinal) { [KeyField] = [message] },
        };
}
