namespace OkulYonetim.Application.Features.Platform;

/// <summary>Platform panelindeki okul özeti (spec 0007 gereksinim 5): okulun içindeki veri yok.</summary>
public sealed record SchoolRow(Guid TenantId, string Name, DateTimeOffset CreatedAt, bool IsActive, int ActiveBranchCount, int ActiveMemberCount, IReadOnlyList<SchoolFounder> Founders);
