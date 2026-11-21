namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Puan listesi kaydı (spec 0013): öğrenci başına puan.</summary>
public sealed record GradesRequest(IReadOnlyList<GradeScoreRequest>? Grades);
