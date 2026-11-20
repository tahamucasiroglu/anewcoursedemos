using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Students.Queries.GetStudent;

/// <summary>Kurucunun öğrenci kartı (bilgiler, sağlık, veliler, kardeşler, teslim kişileri); her okunuşu audit'e geçer.</summary>
public sealed record GetStudentQuery(Guid Id) : IRequest<StudentDetail>;
