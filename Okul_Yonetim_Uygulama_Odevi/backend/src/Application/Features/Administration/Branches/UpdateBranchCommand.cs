using NArchitecture.Core.Mediator;
using NArchitecture.Core.Application.Pipelines.Logging;

namespace OkulYonetim.Application.Features.Administration.Branches;

/// <summary>Şube bilgisini düzenle (ad, adres, telefon).</summary>
public sealed record UpdateBranchCommand(Guid Id, string Name, string? Address, string? Phone) : IRequest<BranchAdminView>, ILoggableRequest;
