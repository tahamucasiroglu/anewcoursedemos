namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Bilgi mesajı yanıtı (ör. "Bu e-posta sistemde kayıtlıysa …").</summary>
public sealed record MessageResponse(string Message);
