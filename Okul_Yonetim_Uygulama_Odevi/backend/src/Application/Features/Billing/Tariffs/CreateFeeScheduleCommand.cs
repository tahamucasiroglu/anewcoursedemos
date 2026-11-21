using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Billing.Tariffs;

/// <summary>Kurucunun yeni tarifesi (spec 0015 gereksinim 1): ad ve tutar (ham metin — "85000.50"). Para taşıdığı için bütünüyle loglanmaz (T9).</summary>
public sealed record CreateFeeScheduleCommand(string? Name, string? Amount) : IRequest<FeeScheduleRow>;
