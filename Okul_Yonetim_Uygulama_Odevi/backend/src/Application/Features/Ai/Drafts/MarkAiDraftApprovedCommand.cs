using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Ai.Drafts;

/// <summary>
/// Diğer modüllerin onay kapısı, 2. adım (spec 0012 C-4, BR-5): insan gönderdiğinde taslak son metin, onaylayan ve sonuç
/// kaydıyla (gönderilen mesaj) "onaylandı" olur. Kaydı çağıran yapar — sonuç kaydıyla aynı işlemde (birim iş). Önce
/// <see cref="GetPendingAiDraftQuery"/> çağrılmış olmalı. Son metin taşıdığı için loglanmaz (T9).
/// </summary>
public sealed record MarkAiDraftApprovedCommand(Guid DraftId, string FinalText, Guid ResultId) : IRequest<bool>;
