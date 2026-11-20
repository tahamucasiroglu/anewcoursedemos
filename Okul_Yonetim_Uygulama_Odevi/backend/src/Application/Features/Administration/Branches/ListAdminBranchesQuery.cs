using NArchitecture.Core.Mediator;
using NArchitecture.Core.Application.Pipelines.Logging;

namespace OkulYonetim.Application.Features.Administration.Branches;

/// <summary>Yönetim ekranı için seçili okulun bütün şubeleri (pasifler dahil): önce etkinler, sonra ada göre.</summary>
public sealed record ListAdminBranchesQuery : IRequest<IReadOnlyList<BranchAdminView>>, ILoggableRequest;
