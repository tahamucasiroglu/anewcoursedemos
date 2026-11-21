using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Billing.Tariffs;

/// <summary>Tarifenin adını ve tutarını değiştirir (spec 0015 gereksinim 1); açılmış planlar değişmez. Loglanmaz (T9).</summary>
public sealed record UpdateFeeScheduleCommand(Guid Id, string? Name, string? Amount) : IRequest<FeeScheduleRow>;
