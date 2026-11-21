namespace OkulYonetim.Application.Features.Academics.Assessments;

/// <summary>Puan kaydının sonucu (spec 0013 gereksinim 6): sınıfta süren öğrenciler arasında puanı girilmiş ve toplam öğrenci sayısı ve bu kayıtta değişen puan sayısı.</summary>
public sealed record GradesSaveResult(int GradedCount, int StudentCount, int Changed);
