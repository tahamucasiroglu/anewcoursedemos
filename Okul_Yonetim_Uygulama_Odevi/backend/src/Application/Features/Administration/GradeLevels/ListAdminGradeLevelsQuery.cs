using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Administration.GradeLevels;

/// <summary>Yönetim ekranı için seçili okulun bütün sınıf seviyeleri (pasifler dahil): sıra numarasına, eşitse ada göre.</summary>
public sealed record ListAdminGradeLevelsQuery : IRequest<IReadOnlyList<GradeLevelAdminView>>, ILoggableRequest;
