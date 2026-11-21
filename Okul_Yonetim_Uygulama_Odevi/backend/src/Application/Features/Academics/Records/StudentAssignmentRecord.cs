namespace OkulYonetim.Application.Features.Academics.Records;

/// <summary>Öğrencinin bir ödevi (spec 0013 gereksinim 8): ders, başlık, teslim günü ve öğrencinin durumu (<c>Unmarked</c>, <c>Submitted</c>, <c>NotSubmitted</c>).</summary>
public sealed record StudentAssignmentRecord(Guid AssignmentId, string Subject, string Title, DateOnly DueDate, string Status);
