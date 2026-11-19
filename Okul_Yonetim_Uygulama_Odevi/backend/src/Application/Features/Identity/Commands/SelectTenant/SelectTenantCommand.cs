using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Identity.Commands.SelectTenant;

/// <summary>Oturumda çalışılacak okulu seçer ya da değiştirir (yalnızca kurum kimliği taşır; loglanabilir).</summary>
public sealed record SelectTenantCommand(Guid TenantId) : IRequest<AuthResult>, ILoggableRequest;
