using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Ai.WeeklySummaries;

/// <summary>
/// Haftalık özet onay kapısı, 2. adım (spec 0014 gereksinim 5, C-1): taslak son metin, onaylayan (oturumdaki öğretmen — oluşturan
/// olmayabilir) ve zamanla "onaylandı" olur; kaydı çağıran yapar (bildirimlerle aynı işlemde). Önce
/// <see cref="GetWeeklySummaryForSendQuery"/> çağrılmış olmalı; öğrenciye erişim yine denetlenir. Son metin taşıdığı için loglanmaz (T9).
/// </summary>
public sealed record ApproveWeeklySummaryDraftCommand(Guid DraftId, string FinalText) : IRequest<bool>;
