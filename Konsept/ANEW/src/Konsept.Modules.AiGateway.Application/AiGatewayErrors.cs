using System.Globalization;
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

    /// <summary>Hata kodu: çağrı için key girilmemiş (spec 0013 Req 1).</summary>
    public const string NoKeyCode = "ai.no_key";

    /// <summary>Hata kodu: key çözülemiyor.</summary>
    public const string KeyUnreadableCode = "ai.key_unreadable";

    /// <summary>Hata kodu: model yetkisi yok.</summary>
    public const string NoPermissionCode = "ai.no_permission";

    /// <summary>Hata kodu: desteklenmeyen efor.</summary>
    public const string EffortUnsupportedCode = "ai.effort_unsupported";

    /// <summary>Hata kodu: sağlayıcı yanıt vermedi (zaman aşımı).</summary>
    public const string ProviderNoResponseCode = "ai.provider_no_response";

    /// <summary>Alan hatası anahtarı (istekteki özellik adı).</summary>
    public const string KeyField = "ApiKey";

    /// <summary>Gets sağlayıcı key'i reddetti; alanın yanında gösterilir.</summary>
    public static Error KeyRejected => FieldError(KeyRejectedCode, AiGatewayText.ApiKeyRejected);

    /// <summary>Gets bilinmeyen sağlayıcı.</summary>
    public static Error UnknownProvider => new(UnknownProviderCode, AiGatewayText.UnknownProvider, ErrorType.NotFound);

    /// <summary>Gets oturum yok.</summary>
    public static Error SignInRequired => new("auth.required", AiGatewayText.SignInRequired, ErrorType.Unauthorized);

    /// <summary>Gets model katalogda yok.</summary>
    public static Error ModelNotFound => new("ai.model_not_found", AiGatewayText.ModelNotFound, ErrorType.NotFound);

    /// <summary>Gets modelin çağrı ucu tanımlı değil.</summary>
    public static Error ModelNotCallable => new("ai.model_not_callable", AiGatewayText.ModelNotCallable, ErrorType.Validation);

    /// <summary>Gets model yetkisi yok (sağlayıcıya gidilmedi).</summary>
    public static Error NoPermission => new(NoPermissionCode, AiGatewayText.CallNoPermission, ErrorType.Validation);

    /// <summary>Gets efor desteklenmiyor (sağlayıcıya gidilmedi).</summary>
    public static Error EffortUnsupported => new(EffortUnsupportedCode, AiGatewayText.CallEffortUnsupported, ErrorType.Validation);

    /// <summary>Gets sağlayıcı key'i reddetti (401).</summary>
    public static Error ProviderKeyRejected => new("ai.provider_key_rejected", AiGatewayText.ProviderKeyRejected, ErrorType.Conflict);

    /// <summary>Gets sağlayıcı yoğun (429/5xx, tekrarlar tükendi).</summary>
    public static Error ProviderBusy => new("ai.provider_busy", AiGatewayText.ProviderBusy, ErrorType.Conflict);

    /// <summary>Gets sağlayıcıdan yanıt alınamadı (tekrar yok).</summary>
    public static Error ProviderNoResponse => new(ProviderNoResponseCode, AiGatewayText.ProviderNoResponse, ErrorType.Conflict);

    /// <summary>Gets sağlayıcıya bağlanılamadı (tekrarlar tükendi).</summary>
    public static Error ProviderUnreachable => new("ai.provider_unreachable", AiGatewayText.ProviderUnreachable, ErrorType.Conflict);

    /// <summary>Gets yetersiz bakiye (402).</summary>
    public static Error ProviderBalance => new("ai.provider_balance", AiGatewayText.ProviderBalance, ErrorType.Conflict);

    /// <summary>Gets sağlayıcı izin vermedi (403).</summary>
    public static Error ProviderForbidden => new("ai.provider_forbidden", AiGatewayText.ProviderForbidden, ErrorType.Conflict);

    /// <summary>Gets sağlayıcı isteği kabul etmedi (400, 404, diğer 4xx).</summary>
    public static Error ProviderRequestRejected => new("ai.provider_rejected", AiGatewayText.ProviderRequestRejected, ErrorType.Conflict);

    /// <summary>Sağlayıcı için key girilmemiş.</summary>
    /// <param name="providerName">Sağlayıcı adı.</param>
    /// <returns>Hata.</returns>
    public static Error NoKey(string providerName) =>
        new(NoKeyCode, string.Format(CultureInfo.InvariantCulture, AiGatewayText.CallNoKey, providerName), ErrorType.Validation);

    /// <summary>Sağlayıcının key'i çözülemiyor.</summary>
    /// <param name="providerName">Sağlayıcı adı.</param>
    /// <returns>Hata.</returns>
    public static Error KeyUnreadable(string providerName) =>
        new(KeyUnreadableCode, string.Format(CultureInfo.InvariantCulture, AiGatewayText.CallKeyUnreadable, providerName), ErrorType.Validation);

    /// <summary>Sağlayıcının son hata durum kodunu kullanıcı mesajına çevirir (ham yanıt asla taşınmaz, Req 9).</summary>
    /// <param name="statusCode">Durum kodu.</param>
    /// <returns>Hata.</returns>
    public static Error ForProviderStatus(int statusCode) => statusCode switch
    {
        401 => ProviderKeyRejected,
        402 => ProviderBalance,
        403 => ProviderForbidden,
        429 or (>= 500 and <= 599) => ProviderBusy,
        _ => ProviderRequestRejected,
    };

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
