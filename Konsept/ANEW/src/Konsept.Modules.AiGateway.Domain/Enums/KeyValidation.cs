namespace Konsept.Modules.AiGateway.Domain.Enums;

/// <summary>Kaydedilen key'in doğrulama etiketi (spec 0004 Req 10).</summary>
public enum KeyValidation
{
    /// <summary>Sağlayıcı maliyetsiz uçta key'i kabul etti ("Doğrulandı").</summary>
    Verified,

    /// <summary>Doğrulama sonuçlanamadı ya da sağlayıcının maliyetsiz yolu yok ("Doğrulanmadı").</summary>
    Unverified,
}
