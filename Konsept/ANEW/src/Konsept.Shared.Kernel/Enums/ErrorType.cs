namespace Konsept.Shared.Kernel.Enums;

/// <summary>
/// Beklenen iş hatalarının türü. HTTP yanıtına çevrilirken durum kodunu belirler.
/// </summary>
public enum ErrorType
{
    /// <summary>Girdi doğrulamadan geçmedi (400).</summary>
    Validation,

    /// <summary>İstenen kayıt bulunamadı (404).</summary>
    NotFound,

    /// <summary>İşlem mevcut durumla çakışıyor (409).</summary>
    Conflict,

    /// <summary>Gereken bir bileşen (veritabanı, ajan) şu an erişilemez (503).</summary>
    Unavailable,

    /// <summary>Beklenen cevap süresinde gelmedi (504).</summary>
    Timeout,
}
