using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Administration.ClassroomTeachers;

/// <summary>
/// Sınıfın öğretmen listesini verilen kişilerle eşitler (spec 0010 gereksinim 1): listede olmayan atamalar kalkar, yeni
/// kişiler atanır. Boş liste bütün atamaları kaldırır. Yalnızca kimlikler taşınır.
/// </summary>
public sealed record SetClassroomTeachersCommand(Guid ClassroomId, IReadOnlyList<Guid>? TeacherUserIds) : IRequest<ClassroomTeachersView>, ILoggableRequest;
