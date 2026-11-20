using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication.WorkingHours;

/// <summary>Mesai saatlerini kaydetme ya da sessiz modu kapatma (<c>Enabled</c> = false): başlangıç ve bitiş "SS:DD" (15 dk adım, başlangıç bitişten önce), günler (<c>Monday</c>…<c>Sunday</c>, en az bir).</summary>
public sealed record SetMyQuietHoursCommand(bool Enabled, string? Start, string? End, IReadOnlyList<string>? Days) : IRequest<QuietHoursView>, ILoggableRequest;
