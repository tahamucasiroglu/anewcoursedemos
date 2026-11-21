namespace OkulYonetim.Application.Features.Organization;

/// <summary>Sınıfın şubesi (spec 0017: sınıf kapsamlı duyuruyu şube süzgecine bağlamak için).</summary>
public sealed record ClassroomBranch(Guid ClassroomId, Guid BranchId);
