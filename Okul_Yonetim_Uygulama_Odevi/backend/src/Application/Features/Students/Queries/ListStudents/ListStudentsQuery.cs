using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Students.Queries.ListStudents;

/// <summary>
/// Kurucunun öğrenci listesi (spec 0009 gereksinim 4): ad veya okul numarasıyla arama, şube, sınıf ve durum
/// (<c>active</c> / <c>left</c>; boşsa hepsi) süzgeci, sayfa başına 25. Arama metni kişisel veri olabileceği için
/// <c>ILoggableRequest</c> değildir.
/// </summary>
public sealed record ListStudentsQuery(string? Search, Guid? BranchId, Guid? ClassroomId, string? Status, int Page) : IRequest<StudentPage>;
