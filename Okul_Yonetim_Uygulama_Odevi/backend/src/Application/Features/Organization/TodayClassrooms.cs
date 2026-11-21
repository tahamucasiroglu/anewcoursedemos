namespace OkulYonetim.Application.Features.Organization;

/// <summary>
/// Okulun bugünü (Türkiye günü), bugün okul günü mü (dönem içinde ve tatil değil — spec 0010 kuralı) ve okul günüyse bugünü kapsayan
/// dönemin etkin şubelerdeki etkin sınıfları (spec 0017 devamsızlık kartı). Okul günü değilse liste boştur.
/// </summary>
public sealed record TodayClassrooms(DateOnly Today, bool IsSchoolDay, IReadOnlyList<TodayClassroom> Classrooms);
