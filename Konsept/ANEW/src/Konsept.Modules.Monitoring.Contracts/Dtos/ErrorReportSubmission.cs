using Konsept.Modules.Monitoring.Contracts.Enums;

namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>Bir ajan bildiriminin sonucu: kabul edildiyse numarası.</summary>
/// <param name="Outcome">Sonuç.</param>
/// <param name="Number">Kabul edildiyse bildirim numarası.</param>
public sealed record ErrorReportSubmission(ErrorReportOutcome Outcome, long? Number = null);
