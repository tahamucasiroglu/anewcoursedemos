namespace TahaKonsept.BuildingBlocks.Domain.Results;

/// <summary>
/// Hata kategorisi; API katmanında HTTP durum koduna çevrilir
/// (Validation→400, NotFound→404, Conflict→409, Unauthorized→401, Forbidden→403, Failure→500).
/// </summary>
public enum ErrorType
{
    /// <summary>Hata yok (başarılı sonuç).</summary>
    None = 0,

    /// <summary>Girdi doğrulama hatası.</summary>
    Validation = 1,

    /// <summary>Kayıt bulunamadı.</summary>
    NotFound = 2,

    /// <summary>Çakışma (ör. aynı kayıt zaten var, sürüm çakışması).</summary>
    Conflict = 3,

    /// <summary>Kimlik doğrulanmadı.</summary>
    Unauthorized = 4,

    /// <summary>Yetki yok.</summary>
    Forbidden = 5,

    /// <summary>Genel / beklenmeyen hata.</summary>
    Failure = 6,
}
