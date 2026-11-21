namespace OkulYonetim.Application.Features.Academics.Assignments;

/// <summary>Teslim kaydının sonucu (spec 0013 gereksinim 4): sınıfta süren öğrenciler arasında teslim eden, etmeyen ve işaretlenmemiş sayısı ve bu kayıtta değişen öğrenci sayısı.</summary>
public sealed record SubmissionsSaveResult(int Submitted, int NotSubmitted, int Unmarked, int Changed);
