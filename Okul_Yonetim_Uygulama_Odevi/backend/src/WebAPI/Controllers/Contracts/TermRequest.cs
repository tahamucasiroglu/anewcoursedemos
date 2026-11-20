namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Dönem ekleme/düzenleme isteği: ad, başlangıç ve bitiş günü (<c>yyyy-MM-dd</c>).</summary>
public sealed record TermRequest(string Name, DateOnly? StartDate, DateOnly? EndDate);
