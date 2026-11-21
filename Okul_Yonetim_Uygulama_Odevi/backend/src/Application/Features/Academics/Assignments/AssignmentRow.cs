namespace OkulYonetim.Application.Features.Academics.Assignments;

/// <summary>Öğretmenin ödev listesindeki satır (spec 0013 gereksinim 3): ders, başlık, teslim günü, veren öğretmen ve "x / y teslim etti" sayıları (yalnızca sınıfta süren öğrenciler).</summary>
public sealed record AssignmentRow(Guid Id, string Subject, string Title, DateOnly DueDate, string CreatedByName, int SubmittedCount, int StudentCount);
