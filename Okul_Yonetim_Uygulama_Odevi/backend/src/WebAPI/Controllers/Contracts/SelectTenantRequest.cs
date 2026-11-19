namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Okul seçimi isteği: seçilecek okulun kimliği.</summary>
public sealed record SelectTenantRequest(Guid TenantId);
