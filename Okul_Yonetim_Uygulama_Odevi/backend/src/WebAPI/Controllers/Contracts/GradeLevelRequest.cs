namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Sınıf seviyesi ekleme/düzenleme isteği: ad ve sıra numarası (1–99).</summary>
public sealed record GradeLevelRequest(string Name, int? SortOrder);
