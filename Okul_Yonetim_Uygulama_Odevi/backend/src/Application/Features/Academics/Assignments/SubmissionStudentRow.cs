namespace OkulYonetim.Application.Features.Academics.Assignments;

/// <summary>Teslim listesindeki öğrenci: kimlik, ad-soyad ve durum (<c>Unmarked</c>, <c>Submitted</c>, <c>NotSubmitted</c>).</summary>
public sealed record SubmissionStudentRow(Guid StudentId, string FullName, string Status);
