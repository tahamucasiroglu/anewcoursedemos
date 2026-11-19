namespace OkulYonetim.Application.Features.Organization;

/// <summary>Şubenin kimliği ve adı (hoş geldin ekranı ve şube listesi).</summary>
public sealed record BranchSummary(Guid Id, string Name);
