using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Ai.WeeklySummaries;

/// <summary>
/// "Özet oluştur" (spec 0014 gereksinim 3, 4, 8; BR-5): dayanaktan yerel modelle 3–5 cümlelik taslak (en fazla 800) — veliye hiçbir şey
/// gitmez; yeniden oluşturma bekleyen taslağın yerine geçer. Öğretmen notu en fazla 500 karakter. Öğretmen notu taşıdığı için loglanmaz (T9).
/// </summary>
public sealed record GenerateWeeklySummaryDraftCommand(Guid StudentId, string? Week, string? TeacherNote) : IRequest<WeeklySummaryPanel>
{
    public const int TeacherNoteMax = 500;
}
