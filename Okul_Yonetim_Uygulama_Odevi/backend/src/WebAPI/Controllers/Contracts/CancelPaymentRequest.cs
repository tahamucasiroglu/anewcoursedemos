namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Tahsilat iptali (spec 0015): gerekçe (zorunlu, en fazla 200 karakter).</summary>
public sealed record CancelPaymentRequest(string? Reason);
