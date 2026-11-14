namespace TahaKonsept.Web.Models;

/// <summary>Hata sayfası görünüm modeli: izleme kimliği ve durum kodu.</summary>
public sealed record ErrorPageModel
{
    /// <summary>İstek izleme kimliği (günlükte aramak için).</summary>
    public string? TraceId { get; init; }

    /// <summary>HTTP durum kodu.</summary>
    public int StatusCode { get; init; } = 500;
}
