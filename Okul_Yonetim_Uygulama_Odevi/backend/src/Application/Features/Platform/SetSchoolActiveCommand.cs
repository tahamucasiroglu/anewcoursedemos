using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Platform;

/// <summary>Okulu pasifleştir / etkinleştir.</summary>
public sealed record SetSchoolActiveCommand(Guid TenantId, bool Active) : IRequest<SchoolRow>;
