using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Students.Commands.UpdateStudent;

/// <summary>
/// Öğrenci bilgilerini düzenle (spec 0009 gereksinim 3): ad-soyad, doğum tarihi, okul numarası ve sınıf (aynı şubenin
/// sınıfı ya da boş). <c>BranchId</c> verilirse kayıttaki şubeyle aynı olmalı — şube değiştirme reddedilir (nakil yok).
/// Sağlık alanları ayrı komutla (<c>SetStudentHealthCommand</c>). Kişisel veri taşır; <c>ILoggableRequest</c> değildir.
/// </summary>
public sealed record UpdateStudentCommand(
    Guid Id,
    string FullName,
    DateOnly? BirthDate,
    string? SchoolNumber,
    Guid? BranchId,
    Guid? ClassroomId) : IRequest<StudentSaved>;
