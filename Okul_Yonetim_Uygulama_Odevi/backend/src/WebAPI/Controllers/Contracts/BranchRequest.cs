namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Şube ekleme/düzenleme isteği: ad (zorunlu), adres ve telefon (isteğe bağlı).</summary>
public sealed record BranchRequest(string Name, string? Address, string? Phone);
