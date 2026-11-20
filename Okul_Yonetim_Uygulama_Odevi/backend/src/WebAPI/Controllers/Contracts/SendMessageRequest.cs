namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Mesaj (spec 0011): metin (boş olamaz, en fazla 2000 karakter).</summary>
public sealed record SendMessageRequest(string? Body);
