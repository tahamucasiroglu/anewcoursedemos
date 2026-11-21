namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Yeni ödev (spec 0013): ders adı, başlık, isteğe bağlı açıklama ve teslim günü (<c>yyyy-MM-dd</c>).</summary>
public sealed record AssignmentRequest(string? Subject, string? Title, string? Description, DateOnly? DueDate);
