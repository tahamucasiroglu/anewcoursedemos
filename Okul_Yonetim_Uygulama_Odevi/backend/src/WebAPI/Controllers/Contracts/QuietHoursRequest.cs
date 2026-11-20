namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>
/// Mesai saatleri (spec 0011): sessiz mod açık mı; açıksa başlangıç ve bitiş ("SS:DD", Türkiye saati, 15 dk adım) ve günler
/// (<c>Monday</c>…<c>Sunday</c>).
/// </summary>
public sealed record QuietHoursRequest(bool Enabled, string? Start, string? End, IReadOnlyList<string>? Days);
