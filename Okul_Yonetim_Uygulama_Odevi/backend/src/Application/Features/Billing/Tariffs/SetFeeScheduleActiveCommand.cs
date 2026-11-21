using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Billing.Tariffs;

/// <summary>Tarifeyi pasifleştirir ya da yeniden etkinleştirir (spec 0015 gereksinim 1); pasif tarife yeni planda seçilemez.</summary>
public sealed record SetFeeScheduleActiveCommand(Guid Id, bool Active) : IRequest<FeeScheduleRow>;
