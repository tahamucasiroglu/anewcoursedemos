namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>
/// Sınıf açma/düzenleme isteği: dönem, şube, seviye ve ad. Düzenlemede yalnızca ad ve seviye kullanılır; dönem ve şube
/// sınıf açıldıktan sonra değişmez.
/// </summary>
public sealed record ClassroomRequest(Guid? TermId, Guid? BranchId, Guid? GradeLevelId, string Name);
