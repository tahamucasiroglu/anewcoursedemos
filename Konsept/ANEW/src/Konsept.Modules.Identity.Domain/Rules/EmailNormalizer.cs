namespace Konsept.Modules.Identity.Domain.Rules;

/// <summary>
/// E-postanın karşılaştırma biçimi (spec 0002 Req 2): baş/son boşluk kırpılır, kültürden bağımsız büyük harfe çevrilir.
/// Türkçe kültürde <c>i</c> → <c>İ</c> tuzağına düşmez (docs/conventions.md, Known traps). Kültürden bağımsız eşleme
/// <c>ı</c> ve <c>İ</c>'yi <c>I</c>'ya katlamaz; bunlar ayrı adres sayılır (plan A-2).
/// </summary>
public static class EmailNormalizer
{
    /// <summary>E-postayı normalleştirir.</summary>
    /// <param name="email">Girilen e-posta.</param>
    /// <returns>Normalleştirilmiş biçim; <c>null</c> için boş metin.</returns>
    public static string Normalize(string? email) =>
        (email ?? string.Empty).Trim().ToUpperInvariant();
}
