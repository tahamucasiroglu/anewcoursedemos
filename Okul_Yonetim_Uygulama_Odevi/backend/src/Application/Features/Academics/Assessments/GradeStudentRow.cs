namespace OkulYonetim.Application.Features.Academics.Assessments;

/// <summary>Puan listesindeki öğrenci: kimlik, ad-soyad ve puan (boş = girilmedi).</summary>
public sealed record GradeStudentRow(Guid StudentId, string FullName, int? Score);
