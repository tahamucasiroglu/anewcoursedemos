namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Yeni değerlendirme (spec 0013): ders adı, tür (<c>Written</c>/<c>Oral</c>), başlık ve gün (<c>yyyy-MM-dd</c>).</summary>
public sealed record AssessmentRequest(string? Subject, string? Kind, string? Title, DateOnly? Date);
