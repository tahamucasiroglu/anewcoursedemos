using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Administration.Terms;

/// <summary>Yönetim ekranı için seçili okulun dönemleri: başlangıç gününe göre yeniden eskiye; güncel/yaklaşan işaretli.</summary>
public sealed record ListAdminTermsQuery : IRequest<IReadOnlyList<TermAdminView>>, ILoggableRequest;
