namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Yenileme bağlantısı denetimi.</summary>
public sealed record ResetLinkRequest(string? Token);
