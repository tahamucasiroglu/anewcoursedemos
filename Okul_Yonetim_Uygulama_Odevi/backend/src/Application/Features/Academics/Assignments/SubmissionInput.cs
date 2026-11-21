namespace OkulYonetim.Application.Features.Academics.Assignments;

/// <summary>Kaydedilen teslim listesinde bir öğrencinin durumu: <c>Unmarked</c>, <c>Submitted</c> ya da <c>NotSubmitted</c>.</summary>
public sealed record SubmissionInput(Guid StudentId, string? Status);
