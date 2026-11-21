namespace OkulYonetim.Application.Features.Dashboard.Summary;

/// <summary>Geciken ödemeler kartında bir öğrenci (spec 0017 gereksinim 4): ad-soyad, şube, sınıf, geciken tutar, en eski geciken vade ve gecikme gün sayısı (bugün − vade).</summary>
public sealed record OverdueStudent(Guid StudentId, string FullName, string BranchName, string? ClassroomName, decimal OverdueAmount, DateOnly OldestDue, int DaysOverdue);
