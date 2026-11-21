using OkulYonetim.Application.Features.Academics.Assignments;

namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Teslim listesi kaydı (spec 0013): öğrenci başına durum (<c>Unmarked</c>, <c>Submitted</c>, <c>NotSubmitted</c>).</summary>
public sealed record SubmissionsRequest(IReadOnlyList<SubmissionInput>? Entries);
