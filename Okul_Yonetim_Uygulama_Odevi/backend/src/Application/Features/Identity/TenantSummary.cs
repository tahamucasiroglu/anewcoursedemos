namespace OkulYonetim.Application.Features.Identity;

/// <summary>Okul seçimi ve hoş geldin ekranı için kurumun kimliği ve adı.</summary>
public sealed record TenantSummary(Guid Id, string Name);
