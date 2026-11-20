using NArchitecture.Core.Mediator;
using NArchitecture.Core.Application.Pipelines.Logging;

namespace OkulYonetim.Application.Features.Administration.Branches;

/// <summary>Şube ekle (spec 0005 gereksinim 2): ad zorunlu, adres ve telefon isteğe bağlı. Kişisel veri taşımaz; loglanır.</summary>
public sealed record CreateBranchCommand(string Name, string? Address, string? Phone) : IRequest<BranchAdminView>, ILoggableRequest;
