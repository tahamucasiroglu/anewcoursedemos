using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Billing.Tariffs;

/// <summary>Okulun tarifeleri (spec 0015): kurucu yönetir, muhasebe plan formu için okur; önce etkinler, Türkçe ad sırasıyla.</summary>
public sealed record ListFeeSchedulesQuery : IRequest<IReadOnlyList<FeeScheduleRow>>;
