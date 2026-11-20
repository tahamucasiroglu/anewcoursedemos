using NArchitecture.Core.Mediator;
using OkulYonetim.Domain.Ai;

namespace OkulYonetim.Application.Features.Ai.Drafts;

/// <summary>
/// Diğer modüllerin onay kapısı, 1. adım (spec 0012 gereksinim 2, 3): taslak oturumdaki kişinin, verilen türde ve verilen
/// bağlamda (ör. aynı konuşma) olmalı — değilse ya da yoksa 404 ("Taslak bulunamadı"); kullanılmışsa "Bu taslak zaten
/// kullanıldı.". Taslağın kimliğini döndürür. Uç yoktur.
/// </summary>
public sealed record GetPendingAiDraftQuery(Guid DraftId, AiDraftKind Kind, Guid ContextId) : IRequest<Guid>;
