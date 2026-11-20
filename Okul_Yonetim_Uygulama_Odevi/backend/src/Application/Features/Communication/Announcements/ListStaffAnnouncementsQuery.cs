using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Web duyuru listesi (spec 0011 UX): kurucuda okulun bütün duyuruları, öğretmende yalnızca kendi yayınladıkları; yeniden eskiye, en fazla 100.</summary>
public sealed record ListStaffAnnouncementsQuery : IRequest<IReadOnlyList<StaffAnnouncementRow>>, ILoggableRequest;
