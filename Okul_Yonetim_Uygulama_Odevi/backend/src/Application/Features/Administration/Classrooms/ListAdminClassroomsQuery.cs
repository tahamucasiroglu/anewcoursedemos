using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Administration.Classrooms;

/// <summary>
/// Yönetim ekranı için bir dönemin sınıfları (dönem verilmezse güncel, yoksa en yeni dönem); isteğe bağlı şube ve durum
/// (<c>Active</c>: etkin/pasif; boşsa hepsi) süzgeci. Şube adı, seviye sırası ve sınıf adıyla dizilir.
/// </summary>
public sealed record ListAdminClassroomsQuery(Guid? TermId, Guid? BranchId, bool? Active) : IRequest<ClassroomAdminList>, ILoggableRequest;
