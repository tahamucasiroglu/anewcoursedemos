namespace OkulYonetim.Application.Features.Academics.Assessments;

/// <summary>Kaydedilen puan listesinde bir öğrencinin puanı, yazıldığı gibi (boş = girilmedi; değilse 0–100 arası tam sayı olmalı).</summary>
public sealed record GradeInput(Guid StudentId, string? Score);
