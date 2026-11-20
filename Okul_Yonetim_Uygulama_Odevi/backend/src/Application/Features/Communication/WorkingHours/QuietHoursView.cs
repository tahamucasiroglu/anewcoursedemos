namespace OkulYonetim.Application.Features.Communication.WorkingHours;

/// <summary>Mesai saatleri: sessiz mod açık mı, başlangıç ve bitiş ("SS:DD", Türkiye saati) ve günler.</summary>
public sealed record QuietHoursView(bool Enabled, string? Start, string? End, IReadOnlyList<string> Days);
