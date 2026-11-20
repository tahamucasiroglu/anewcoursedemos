using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Administration.ClassroomTeachers;

/// <summary>Sınıfın öğretmenleri ve aday öğretmenler (spec 0010); yalnızca kurucu. Kişisel veri taşımaz (yalnızca sınıf kimliği).</summary>
public sealed record ListClassroomTeachersQuery(Guid ClassroomId) : IRequest<ClassroomTeachersView>, ILoggableRequest;
