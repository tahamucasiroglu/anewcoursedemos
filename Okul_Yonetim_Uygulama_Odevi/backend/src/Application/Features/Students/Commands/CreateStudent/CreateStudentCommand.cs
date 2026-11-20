using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Students.Commands.CreateStudent;

/// <summary>
/// Öğrenci ekle (spec 0009 gereksinim 3): ad-soyad, doğum tarihi, isteğe bağlı okul numarası, şube, isteğe bağlı sınıf,
/// sağlık notu ve alerji. Bilinçli olarak <c>ILoggableRequest</c> değildir: kişisel ve sağlık verisi taşır (S2, T9).
/// </summary>
public sealed record CreateStudentCommand(
    string FullName,
    DateOnly? BirthDate,
    string? SchoolNumber,
    Guid? BranchId,
    Guid? ClassroomId,
    string? HealthNote,
    string? Allergy) : IRequest<StudentSaved>;
