namespace OkulYonetim.Domain.Academics;

/// <summary>Ödevde bir öğrencinin teslim durumu (spec 0013 C-2): başlangıçta "İşaretlenmedi"; öğretmen "Teslim etti" ya da "Teslim etmedi" işaretler.</summary>
public enum SubmissionStatus
{
    Unmarked = 0,
    Submitted = 1,
    NotSubmitted = 2,
}
