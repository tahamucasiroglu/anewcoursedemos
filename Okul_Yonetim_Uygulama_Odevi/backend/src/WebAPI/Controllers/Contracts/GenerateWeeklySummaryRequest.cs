namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>"Özet oluştur" (spec 0014): öğretmenin isteğe bağlı kısa notu (en fazla 500 karakter).</summary>
public sealed record GenerateWeeklySummaryRequest(string? TeacherNote);
