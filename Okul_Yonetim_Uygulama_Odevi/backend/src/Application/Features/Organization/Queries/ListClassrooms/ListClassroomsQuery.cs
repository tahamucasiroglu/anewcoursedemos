using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Organization.Queries.ListClassrooms;

/// <summary>
/// Kişinin görebildiği etkin sınıflar (dönem verilmezse güncel, yoksa en yeni dönem): kurucu bütün şubelerin, diğer roller
/// yalnızca atandıkları etkin şubelerin sınıflarını alır. Öğrenci ve yoklama spec'lerinin okuma ucu.
/// </summary>
public sealed record ListClassroomsQuery(Guid? TermId) : IRequest<IReadOnlyList<ClassroomSummary>>, ILoggableRequest;
